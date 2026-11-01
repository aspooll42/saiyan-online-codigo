using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Core;
using Intersect.Framework.SystemInformation;
using Intersect.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.AssetManagement;

public sealed class Updater
{
	public const string KeywordAppManifest = "#appmanifest";

	private const long FailedRetryLimit = 5L;

	private const int SansDonneesMaxMs = 30000;

	internal const int MaxBuffer = 1048576;

	private static readonly string[] SizeSuffixes = new string[5] { "B", "KiB", "MiB", "GiB", "TiB" };

	private readonly ConcurrentQueue<UpdateManifestFile> _downloadQueue = new ConcurrentQueue<UpdateManifestFile>();

	private readonly ConcurrentDictionary<UpdateManifestFile, DownloadResult> _downloadResults = new ConcurrentDictionary<UpdateManifestFile, DownloadResult>();

	private readonly DirectoryInfo _updateRootInfo = new DirectoryInfo(Path.GetFullPath(".", Environment.CurrentDirectory));

	private readonly string _versionPath;

	private readonly string? _baseUrl;

	private readonly string? _manifestUrl;

	private readonly Thread? _mainThread;

	private readonly int _workerThreadCount = 1;

	private Thread[] _workerThreads;

	private ulong _activeThreads;

	private long _downloadedBytes;

	private bool _failed;

	private bool _stopping;

	private bool _updaterContentChanged;

	private Dictionary<string, UpdateManifestFile>? _manifesteInstalle;

	private DateTime _manifesteInstalleEcrit;

	private UpdateManifest? _fetchedManifest;

	private UpdaterStatus _updaterStatus;

	private TokenResponse? _tokenResponse;

	private long _sizeTotal;

	public long BytesDownloaded => _downloadedBytes + _downloadResults.Sum((KeyValuePair<UpdateManifestFile, DownloadResult> pair) => (pair.Value.State != DownloadState.Active) ? 0 : pair.Value.Count);

	public Exception Exception { get; private set; }

	public int FilesRemaining => _downloadQueue.Count + _downloadResults.Count((KeyValuePair<UpdateManifestFile, DownloadResult> p) => p.Value.State == DownloadState.Active);

	public float Progress => (float)BytesDownloaded / (float)SizeTotal * 100f;

	public bool ShouldRestart { get; private set; }

	public long SizeRemaining => SizeTotal - BytesDownloaded;

	public long SizeTotal => _sizeTotal;

	public UpdateStatus Status { get; private set; } = UpdateStatus.DownloadingManifest;

	public Updater(string manifestUrl, string applicationManifestFileName, string currentVersionPath, int maxDownloadThreads = 10)
	{
		if (string.IsNullOrWhiteSpace(manifestUrl))
		{
			return;
		}
		_workerThreadCount = maxDownloadThreads;
		_versionPath = currentVersionPath;
		_mainThread = new Thread(RunSynchronously)
		{
			IsBackground = true
		};
		if (manifestUrl.EndsWith('/'))
		{
			_baseUrl = manifestUrl;
		}
		string text = manifestUrl.TrimEnd('/');
		if (text.EndsWith("#appmanifest"))
		{
			text = text.Replace("#appmanifest", applicationManifestFileName);
		}
		_manifestUrl = text;
		if (_baseUrl == null)
		{
			Uri uri = new Uri(text);
			string[] value = (from segment in uri.Segments.Take(Math.Max(0, uri.Segments.Length - 1))
				select segment.TrimEnd('/') into segment
				where !string.IsNullOrWhiteSpace(segment)
				select segment).ToArray();
			UriBuilder uriBuilder = new UriBuilder(uri)
			{
				Path = string.Join('/', value)
			};
			uri = uriBuilder.Uri;
			_baseUrl = uri.AbsoluteUri.TrimEnd('/');
		}
	}

	public void SetAuthorizationData(TokenResponse tokenResponse)
	{
		_tokenResponse = tokenResponse;
	}

	public UpdaterStatus TryGetManifest(out UpdateManifest? updateManifest, int reattemptTimes = 0, bool force = false)
	{
		if (!force)
		{
			if (string.IsNullOrWhiteSpace(_manifestUrl))
			{
				updateManifest = null;
				return UpdaterStatus.NoUpdateNeeded;
			}
			if (_fetchedManifest != null)
			{
				updateManifest = _fetchedManifest;
				return _updaterStatus;
			}
		}
		try
		{
			using IntersectHttpClient intersectHttpClient = new IntersectHttpClient(_baseUrl, _tokenResponse);
			HttpResponseMessage httpResponseMessage = null;
			Exception[] array = new Exception[1 + reattemptTimes];
			for (int i = 0; i < array.Length; i++)
			{
				try
				{
					KeyValuePair<string, string>[] source = new KeyValuePair<string, string>[2]
					{
						new KeyValuePair<string, string>("token", Environment.TickCount.ToString()),
						new KeyValuePair<string, string>("rid", PlatformInformation.RuntimeIdentifier)
					};
					UriBuilder uriBuilder = new UriBuilder(_manifestUrl ?? throw new InvalidOperationException())
					{
						Query = string.Join("&", source.Select((KeyValuePair<string, string> p) => string.Join('=', Uri.EscapeDataString(p.Key), Uri.EscapeDataString(p.Value))))
					};
					Uri uri = uriBuilder.Uri;
					using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
					ApplicationContext.CurrentContext.Logger.LogInformation("Attempting to fetch update manifest from {ManifestUrl}", _manifestUrl);
					httpResponseMessage = intersectHttpClient.Send(request);
					HttpStatusCode statusCode = httpResponseMessage.StatusCode;
					if (statusCode >= HttpStatusCode.BadRequest)
					{
						if (statusCode == HttpStatusCode.Unauthorized)
						{
							updateManifest = null;
							return UpdaterStatus.NeedsAuthentication;
						}
						continue;
					}
				}
				catch (Exception ex)
				{
					array[i] = ex;
					Thread.Sleep(5000);
					continue;
				}
				break;
			}
			if (httpResponseMessage == null)
			{
				throw new AggregateException(array);
			}
			using (httpResponseMessage)
			{
				using Stream stream = httpResponseMessage.Content.ReadAsStream();
				using StreamReader streamReader = new StreamReader(stream);
				string value = streamReader.ReadToEnd();
				updateManifest = JsonConvert.DeserializeObject<UpdateManifest>(value);
				if (updateManifest == null)
				{
					throw new NullReferenceException("No update manifest from remote server");
				}
				bool flag = !updateManifest.TrustCache;
				string fullPath = Path.GetFullPath(_versionPath, _updateRootInfo.FullName);
				FileInfo fileInfo = new FileInfo(fullPath);
				if (!flag)
				{
					if (fileInfo.Exists)
					{
						Dictionary<string, UpdateManifestFile> updateFileLookup = updateManifest.Files.ToDictionary((UpdateManifestFile umf) => umf.Path, (UpdateManifestFile umf) => umf);
						try
						{
							string value2 = File.ReadAllText(fileInfo.FullName);
							UpdateManifest updateManifest2 = JsonConvert.DeserializeObject<UpdateManifest>(value2);
							if (updateManifest2 == null)
							{
								fileInfo.Delete();
							}
							else
							{
								flag = false;
								Dictionary<string, UpdateManifestFile> dictionary = (_manifesteInstalle = updateManifest2.Files.ToDictionary((UpdateManifestFile umf) => umf.Path, (UpdateManifestFile umf) => umf));
								_manifesteInstalleEcrit = fileInfo.LastWriteTimeUtc;
								foreach (UpdateManifestFile file in updateManifest.Files)
								{
									if (!dictionary.TryGetValue(file.Path, out var value3))
									{
										flag = true;
										break;
									}
									if (value3.Size != file.Size)
									{
										flag = true;
										break;
									}
									if (!string.Equals(file.Checksum, value3.Checksum, StringComparison.Ordinal))
									{
										flag = true;
										break;
									}
									string fileName = ResolvePath(value3.Path, _updateRootInfo.FullName);
									FileInfo local = new FileInfo(fileName);
									if (!EstIntact(file, local))
									{
										flag = true;
										break;
									}
								}
								IEnumerable<UpdateManifestFile> enumerable = updateManifest2.Files.Where((UpdateManifestFile f) => !updateFileLookup.ContainsKey(f.Path));
								foreach (UpdateManifestFile item in enumerable)
								{
									string path = ResolvePath(item.Path, _updateRootInfo.FullName);
									if (File.Exists(path))
									{
										try
										{
											File.Delete(path);
										}
										catch (Exception exception)
										{
											ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Failed to delete cached file: \"{CacheFilePath}\"", item.Path);
										}
									}
								}
							}
						}
						catch (Exception exception2)
						{
							ApplicationContext.CurrentContext.Logger.LogWarning(exception2, "Error inspecting existing manifest, forcing update");
							flag = true;
						}
					}
					else
					{
						flag = true;
					}
				}
				_fetchedManifest = updateManifest;
				_updaterStatus = ((!flag) ? UpdaterStatus.NoUpdateNeeded : UpdaterStatus.Ready);
				return _updaterStatus;
			}
		}
		catch (Exception exception3)
		{
			ApplicationContext.CurrentContext.Logger.LogError(exception3, "Failed to access update server");
			updateManifest = null;
			return UpdaterStatus.Offline;
		}
	}

	public Task Start(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_mainThread == null)
		{
			return Task.CompletedTask;
		}
		return Task.Run(delegate
		{
			_mainThread.Start(cancellationToken);
			_mainThread.Join();
		}, cancellationToken);
	}

	private void RunSynchronously(object? boxedCancellationToken)
	{
		CancellationToken cancellationToken = ((boxedCancellationToken is CancellationToken cancellationToken2) ? cancellationToken2 : default(CancellationToken));
		try
		{
			RunUpdates(cancellationToken);
		}
		catch (Exception ex)
		{
			Exception exception = (Exception = ex);
			Status = UpdateStatus.Error;
			ApplicationContext.CurrentContext.Logger.LogError(exception, "Update failed");
		}
	}

	private void RunUpdates(CancellationToken cancellationToken)
	{
		DeleteTemporaryFiles();
		UpdateManifest updateManifest;
		UpdateManifest updateManifest2;
		try
		{
			UpdaterStatus updaterStatus = TryGetManifest(out updateManifest, 5);
			switch (updaterStatus)
			{
			case UpdaterStatus.Offline:
				throw new Exception("Failed to download update manifest");
			case UpdaterStatus.NoUpdateNeeded:
				Status = UpdateStatus.None;
				ApplicationContext.CurrentContext.Logger.LogInformation("Assets are already up-to-date");
				return;
			case UpdaterStatus.NeedsAuthentication:
				throw new Exception("Not authenticated");
			default:
				throw new UnreachableException();
			case UpdaterStatus.Ready:
			{
				if (updateManifest == null)
				{
					throw new InvalidOperationException($"Manifest is null despite a status of {updaterStatus}");
				}
				updateManifest2 = new UpdateManifest
				{
					StreamingUrl = updateManifest.StreamingUrl,
					TrustCache = updateManifest.TrustCache
				};
				List<UpdateManifestFile> list = new List<UpdateManifestFile>();
				foreach (UpdateManifestFile file in updateManifest.Files)
				{
					if (_stopping)
					{
						return;
					}
					string fileName = ResolvePath(file.Path, _updateRootInfo.FullName);
					FileInfo local = new FileInfo(fileName);
					if (EstIntact(file, local))
					{
						updateManifest2.Files.Add(file);
						continue;
					}
					if (IsUpdaterFile(file.Path))
					{
						EnqueueLocked(file);
						Interlocked.Add(ref _sizeTotal, file.Size);
					}
					list.Add(file);
				}
				foreach (UpdateManifestFile item in list)
				{
					_downloadQueue.Contains(item);
					DownloadResult value = new DownloadResult
					{
						OverrideBaseUrl = updateManifest.OverrideBaseUrl,
						State = DownloadState.Queued
					};
					if (!_downloadResults.TryAdd(item, value))
					{
						ApplicationContext.CurrentContext.Logger.LogWarning("Failed to add file to download results, it may already be registered? {OutputFilePath}", item.Path);
					}
					if (!IsUpdaterFile(item.Path))
					{
						EnqueueLocked(item);
						Interlocked.Add(ref _sizeTotal, item.Size);
					}
				}
				if (_downloadQueue.IsEmpty)
				{
					EcrireManifesteInstalle(updateManifest2);
					Status = UpdateStatus.None;
					return;
				}
				Status = UpdateStatus.UpdateInProgress;
				break;
			}
			}
		}
		catch (Exception ex)
		{
			Status = UpdateStatus.Error;
			Exception = new Exception("[Update Check Failed!] - " + ex.Message, ex);
			ApplicationContext.CurrentContext.Logger.LogError(ex, "Update check failed");
			return;
		}
		Status = UpdateStatus.UpdateInProgress;
		bool flag = false;
		if (!string.IsNullOrWhiteSpace(updateManifest.StreamingUrl))
		{
			flag = StreamDownloads(updateManifest, cancellationToken);
		}
		if (!flag)
		{
			int num = Math.Clamp(Math.Min(_workerThreadCount, _downloadQueue.Count), 1, Math.Max(1, Environment.ProcessorCount - 2));
			_workerThreads = new Thread[num];
			int workerThreadIndex;
			for (workerThreadIndex = 0; workerThreadIndex < num; workerThreadIndex++)
			{
				_workerThreads[workerThreadIndex] = new Thread((ThreadStart)delegate
				{
					Interlocked.Increment(ref _activeThreads);
					_failed |= !DownloadUpdates(workerThreadIndex, cancellationToken);
					Interlocked.Decrement(ref _activeThreads);
				});
				_workerThreads[workerThreadIndex].Start();
			}
		}
		Thread[] workerThreads = _workerThreads;
		foreach (Thread thread in workerThreads)
		{
			thread.Join();
		}
		UpdateManifestFile key;
		DownloadResult value2;
		if (!_failed)
		{
			foreach (KeyValuePair<UpdateManifestFile, DownloadResult> downloadResult3 in _downloadResults)
			{
				downloadResult3.Deconstruct(out key, out value2);
				UpdateManifestFile updateManifestFile = key;
				DownloadResult downloadResult = value2;
				if (downloadResult.State == DownloadState.Failed)
				{
					ApplicationContext.CurrentContext.Logger.LogError("Download of {File} marked as {DownloadState}, marking update as failed", updateManifestFile.Path, downloadResult.State);
					_failed = true;
				}
			}
		}
		if (_failed)
		{
			Status = UpdateStatus.Error;
			return;
		}
		lock (updateManifest2)
		{
			ApplicationContext.CurrentContext.Logger.LogInformation("Collating downloads...");
			lock (_downloadResults)
			{
				foreach (KeyValuePair<UpdateManifestFile, DownloadResult> downloadResult4 in _downloadResults)
				{
					downloadResult4.Deconstruct(out key, out value2);
					UpdateManifestFile updateManifestFile2 = key;
					DownloadResult downloadResult2 = value2;
					if (downloadResult2.State == DownloadState.Completed)
					{
						ApplicationContext.CurrentContext.Logger.LogInformation("Completed download : {OutputFilePath}", updateManifestFile2.Path);
						updateManifest2.Files.Add(updateManifestFile2);
					}
					else
					{
						ApplicationContext.CurrentContext.Logger.LogWarning("Download marked as {ActualDownloadState} instead of {ExpectedDownloadState}: {OutputFilePath}", updateManifestFile2.Path, downloadResult2.State, DownloadState.Completed);
					}
				}
			}
			ApplicationContext.CurrentContext.Logger.LogInformation("Finished downloads, now writing updated version manifest...");
			EcrireManifesteInstalle(updateManifest2);
			ApplicationContext.CurrentContext.Logger.LogInformation("Finished writing updated version manifest...");
		}
		if (!_stopping)
		{
			Status = (ShouldRestart ? UpdateStatus.Restart : UpdateStatus.UpdateCompleted);
		}
	}

	private bool StreamDownloads(UpdateManifest updateManifest, CancellationToken cancellationToken = default(CancellationToken))
	{
		using IntersectHttpClient intersectHttpClient = new IntersectHttpClient(_baseUrl, _tokenResponse);
		List<UpdateManifestFile> list = new List<UpdateManifestFile>();
		List<UpdateManifestFile> list2 = new List<UpdateManifestFile>();
		UpdateManifestFile currentFile;
		DownloadResult value;
		while (!_downloadQueue.IsEmpty && TryDequeueLocked(out currentFile))
		{
			if (!_downloadResults.TryGetValue(currentFile, out value))
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Skipping download of unregistered file in {ManifestPropertyName}: {FilePath}", "OverrideBaseUrl", currentFile.Path);
				_downloadResults[currentFile] = new DownloadResult
				{
					State = DownloadState.Queued
				};
			}
			if (currentFile.Size < updateManifest.StreamingSizeCutoff || IsUpdaterFile(currentFile.Path))
			{
				list.Add(currentFile);
			}
			else
			{
				list2.Add(currentFile);
			}
		}
		foreach (UpdateManifestFile item in list2)
		{
			EnqueueLocked(item);
		}
		string text = $"{updateManifest.StreamingUrl}?token={Environment.TickCount}";
		Uri uri;
		try
		{
			uri = new Uri(text);
			if (uri.Scheme.StartsWith("file"))
			{
				throw new InvalidOperationException("Cannot stream from a file: URI");
			}
		}
		catch (Exception ex)
		{
			if (_baseUrl == null)
			{
				ApplicationContext.CurrentContext.Logger.LogWarning(ex, "No base URL");
				return false;
			}
			try
			{
				Uri baseUri = new Uri(_baseUrl);
				uri = new Uri(baseUri, text);
			}
			catch (Exception ex2)
			{
				AggregateException exception = new AggregateException(ex, ex2);
				ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Unable to resolve streaming url: {StreamingURL}", text);
				return false;
			}
		}
		try
		{
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri.AbsoluteUri);
			string[] value2 = list.Select((UpdateManifestFile f) => f.Path).ToArray();
			string content = JsonConvert.SerializeObject(value2);
			httpRequestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
			using HttpResponseMessage httpResponseMessage = intersectHttpClient.Send(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if (httpResponseMessage.StatusCode == HttpStatusCode.PartialContent)
			{
				return false;
			}
			using Stream input = httpResponseMessage.Content.ReadAsStream(cancellationToken);
			using BinaryReader binaryReader = new BinaryReader(input);
			byte[] array = new byte[1048576];
			long num = httpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault();
			while (!cancellationToken.IsCancellationRequested && num > 0)
			{
				int num2 = 0;
				string assetName;
				try
				{
					byte[] array2 = binaryReader.ReadBytes(4);
					num -= array2.Length;
					int num3 = BinaryPrimitives.ReadInt32LittleEndian(array2);
					byte[] bytes = binaryReader.ReadBytes(num3);
					num -= num3;
					assetName = Encoding.UTF8.GetString(bytes);
				}
				catch
				{
					throw;
				}
				UpdateManifestFile updateManifestFile = _downloadResults.Keys.FirstOrDefault((UpdateManifestFile updateManifestFile3) => string.Equals(updateManifestFile3.Path, assetName));
				if (updateManifestFile == null)
				{
					ApplicationContext.CurrentContext.Logger.LogError("Streaming server returned unknown file, falling back to per-file downloads: " + assetName);
					return false;
				}
				if (!_downloadResults.TryGetValue(updateManifestFile, out var value3))
				{
					ApplicationContext.CurrentContext.Logger.LogError("Unregistered file, falling back to per-file downloads: " + assetName);
					return false;
				}
				if (value3.State != DownloadState.Queued)
				{
					ApplicationContext.CurrentContext.Logger.LogError("Another thread is already downloading this, falling back to per-file downloads: " + assetName);
					return false;
				}
				list.Remove(updateManifestFile);
				byte[] array3 = binaryReader.ReadBytes(8);
				int num4 = (int)BinaryPrimitives.ReadInt64LittleEndian(array3);
				num -= array3.Length;
				string fileName = ResolvePath(updateManifestFile.Path, _updateRootInfo.FullName);
				FileInfo fileInfo = new FileInfo(fileName);
				FileInfo fileInfo2 = new FileInfo($"{fileInfo}.tmp");
				DirectoryInfo directory = fileInfo.Directory;
				if (directory != null && !directory.Exists)
				{
					directory.Create();
				}
				using FileStream fileStream = fileInfo2.OpenWrite();
				while (value3.Count < num4)
				{
					cancellationToken.ThrowIfCancellationRequested();
					int count = (int)Math.Min(Math.Min(num4 - value3.Count, array.Length), 1048576 - num2);
					int num5 = binaryReader.Read(array, num2, count);
					num -= num5;
					num2 += num5;
					value3.Count += num5;
					if (num2 >= 1048576)
					{
						fileStream.Write(array, 0, num2);
						num2 = 0;
					}
				}
				if (num2 > 0)
				{
					fileStream.Write(array, 0, num2);
				}
				fileStream.Close();
				string temporaryChecksum;
				DownloadValidity downloadValidity = ValidateTemporaryFile(updateManifestFile, fileInfo2, out temporaryChecksum);
				string relativePath = Path.GetRelativePath(_updateRootInfo.FullName, fileInfo2.FullName);
				string relativePath2 = Path.GetRelativePath(_updateRootInfo.FullName, fileInfo.FullName);
				if (downloadValidity != DownloadValidity.Valid)
				{
					Exception = new InvalidDataException(downloadValidity switch
					{
						DownloadValidity.Missing => "Temporary cache file missing: " + relativePath, 
						DownloadValidity.LengthMismatch => $"Expected {updateManifestFile.Size} bytes but received {fileInfo2.Length}: {relativePath2}", 
						DownloadValidity.ChecksumMismatch => $"Checksum mismatch, expected {updateManifestFile.Checksum} but received {temporaryChecksum}: {relativePath}", 
						DownloadValidity.Valid => throw new UnreachableException(), 
						_ => throw new UnreachableException(), 
					});
					ApplicationContext.CurrentContext.Logger.LogError(Exception, "Error detected while downloading streamed asset");
					_failed = true;
					Status = UpdateStatus.Error;
					return false;
				}
				if (IsSelf(updateManifestFile.Path))
				{
					ShouldRestart = true;
					try
					{
						if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						{
							fileInfo2.UnixFileMode |= UnixFileMode.UserExecute;
						}
					}
					catch (Exception exception2)
					{
						ApplicationContext.CurrentContext.Logger.LogWarning(exception2, "Failed to make " + fileInfo2.Name + " executable");
					}
				}
				try
				{
					if (fileInfo.Exists)
					{
						ApplicationContext.CurrentContext.Logger.LogTrace("Attempting to delete: {TargetFilePath}", relativePath2);
						fileInfo.Delete();
					}
				}
				catch (Exception exception3)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning(exception3, "Failed to delete, falling back to rename {RelativeTargetPath}", relativePath2);
					FileInfo fileInfo3 = new FileInfo(fileInfo.FullName + ".old");
					try
					{
						FileInfo fileInfo4 = new FileInfo(fileInfo.FullName);
						fileInfo4.MoveTo(fileInfo3.FullName, overwrite: true);
					}
					catch (Exception exception4)
					{
						ApplicationContext.CurrentContext.Logger.LogWarning(exception4, "Failed to rename {RelativeSourcePath} to {RelativeTargetPath}.old", relativePath2, relativePath2);
					}
				}
				try
				{
					fileInfo2.MoveTo(fileInfo.FullName, overwrite: true);
					if (IsUpdaterFile(updateManifestFile.Path))
					{
						_updaterContentChanged = true;
					}
					value3.State = DownloadState.Completed;
					_downloadedBytes += updateManifestFile.Size;
					list.Remove(updateManifestFile);
				}
				catch (Exception exception5)
				{
					ApplicationContext.CurrentContext.Logger.LogTrace(exception5, "Failed to move {SourcePath} to {TargetPath}", fileInfo2.FullName, fileInfo.FullName);
					ApplicationContext.CurrentContext.Logger.LogError(exception5, "Failed to move {RelativeSourcePath} to {RelativeTargetPath}", relativePath, relativePath2);
					try
					{
						using (FileStream fileStream2 = fileInfo2.OpenRead())
						{
							using FileStream destination = fileInfo.OpenWrite();
							fileStream2.CopyTo(destination);
						}
						fileInfo2.Delete();
					}
					catch (Exception)
					{
						ApplicationContext.CurrentContext.Logger.LogError(exception5, "Failed to copy data from {RelativeSourcePath} to {RelativeTargetPath}", relativePath, relativePath2);
					}
				}
			}
			return false;
		}
		catch (EndOfStreamException exception6)
		{
			ApplicationContext.CurrentContext.Logger.LogError(exception6, "Error download from stream: {RequestURI}", uri);
			foreach (KeyValuePair<UpdateManifestFile, DownloadResult> downloadResult2 in _downloadResults)
			{
				downloadResult2.Deconstruct(out var key, out value);
				UpdateManifestFile updateManifestFile2 = key;
				DownloadResult downloadResult = value;
				if (downloadResult.State == DownloadState.Failed)
				{
					EnqueueLocked(updateManifestFile2);
					downloadResult.State = DownloadState.Queued;
				}
			}
			return false;
		}
		catch (Exception exception7)
		{
			ApplicationContext.CurrentContext.Logger.LogError(exception7, "Error download from stream");
			return false;
		}
		finally
		{
			foreach (UpdateManifestFile item2 in list)
			{
				if (!_downloadResults.TryGetValue(item2, out var value4) || value4.State != DownloadState.Completed)
				{
					EnqueueLocked(item2);
				}
			}
		}
	}

	private void EnqueueLocked(UpdateManifestFile updateManifestFile)
	{
		lock (_downloadQueue)
		{
			if (!_downloadQueue.Contains(updateManifestFile))
			{
				_downloadQueue.Enqueue(updateManifestFile);
			}
		}
	}

	private bool TryDequeueLocked([NotNullWhen(true)] out UpdateManifestFile currentFile)
	{
		lock (_downloadQueue)
		{
			return _downloadQueue.TryDequeue(out currentFile) && currentFile != null;
		}
	}

	private bool DownloadUpdates(int workerThreadIndex, CancellationToken cancellationToken = default(CancellationToken))
	{
		using IntersectHttpClient intersectHttpClient = new IntersectHttpClient(_baseUrl, _tokenResponse);
		byte[] array = new byte[1048576];
		UpdateManifestFile currentFile;
		while (!_failed && TryDequeueLocked(out currentFile))
		{
			int num = 0;
			DownloadResult value;
			lock (_downloadResults)
			{
				if (!_downloadResults.TryGetValue(currentFile, out value))
				{
					ApplicationContext.CurrentContext.Logger.LogWarning("Tried to download a file that was not registered, will ignore OverrideBaseUrl: " + currentFile.Path);
					value = new DownloadResult
					{
						State = DownloadState.Active
					};
					_downloadResults[currentFile] = value;
				}
				switch (value.State)
				{
				case DownloadState.Failed:
					if ((long)value.Essais < 5L)
					{
						break;
					}
					goto end_IL_002f;
				case DownloadState.Active:
				case DownloadState.Completed:
					goto end_IL_002f;
				}
				ApplicationContext.CurrentContext.Logger.LogInformation($"[{workerThreadIndex}] Starting download for {value.State} {currentFile.Path}");
				value.Count = 0L;
				value.State = DownloadState.Active;
				goto IL_014b;
				end_IL_002f:;
			}
			continue;
			IL_014b:
			try
			{
				string fileName = ResolvePath(currentFile.Path, _updateRootInfo.FullName);
				FileInfo fileInfo = new FileInfo(fileName);
				FileInfo fileInfo2 = new FileInfo($"{fileInfo}.tmp");
				DirectoryInfo directory = fileInfo.Directory;
				if (directory != null && !directory.Exists)
				{
					directory.Create();
				}
				string value2 = value.OverrideBaseUrl ?? _baseUrl;
				string requestUri = $"{value2}/{currentFile.Path}?token={Environment.TickCount}";
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
				using HttpResponseMessage httpResponseMessage = intersectHttpClient.Send(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
				if (httpResponseMessage.StatusCode == HttpStatusCode.PartialContent)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning($"Unsupported response from update server for \"{currentFile.Path}\" ({httpResponseMessage.StatusCode})");
					value.State = DownloadState.Failed;
					value.Essais = 5;
					continue;
				}
				if (httpResponseMessage.StatusCode >= HttpStatusCode.InternalServerError)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning($"Server error response from update server for \"{currentFile.Path}\" ({httpResponseMessage.StatusCode})");
					value.State = DownloadState.Failed;
					if ((long)(++value.Essais) < 5L)
					{
						Thread.Sleep(2000);
						EnqueueLocked(currentFile);
					}
					continue;
				}
				if (httpResponseMessage.StatusCode >= HttpStatusCode.BadRequest)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning($"Client error response from update server for \"{currentFile.Path}\" ({httpResponseMessage.StatusCode})");
					value.State = DownloadState.Failed;
					value.Essais = 5;
					continue;
				}
				if (currentFile.Size != httpResponseMessage.Content.Headers.ContentLength)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning($"Update stream mismatch for \"{currentFile.Path}\" (expected {currentFile.Size} but received {httpResponseMessage.Content.Headers.ContentLength})");
					value.State = DownloadState.Failed;
					if ((long)(++value.Essais) < 5L)
					{
						EnqueueLocked(currentFile);
					}
					continue;
				}
				using Stream flux = httpResponseMessage.Content.ReadAsStream(cancellationToken);
				if (value.State == DownloadState.Completed)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning($"[{workerThreadIndex}] Two threads were downloading {value.State} {currentFile.Path}");
					continue;
				}
				ApplicationContext.CurrentContext.Logger.LogDebug($"[{workerThreadIndex}] Creating temporary file for {value.State} {currentFile.Path}");
				using FileStream fileStream = fileInfo2.Open(FileMode.Create, FileAccess.Write);
				while (value.Count < currentFile.Size)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						cancellationToken.ThrowIfCancellationRequested();
					}
					int taille = (int)Math.Min(Math.Min(currentFile.Size - value.Count, array.Length), 1048576 - num);
					int num2 = LireOuAbandonner(flux, array, num, taille, cancellationToken);
					if (num2 <= 0)
					{
						throw new EndOfStreamException($"Connection closed after {value.Count} of {currentFile.Size} bytes: {currentFile.Path}");
					}
					num += num2;
					value.Count += num2;
					if (num >= 1048576)
					{
						fileStream.Write(array, 0, num);
						num = 0;
					}
				}
				if (num > 0)
				{
					fileStream.Write(array, 0, num);
				}
				fileStream.Close();
				string temporaryChecksum;
				DownloadValidity downloadValidity = ValidateTemporaryFile(currentFile, fileInfo2, out temporaryChecksum);
				string relativePath = Path.GetRelativePath(_updateRootInfo.FullName, fileInfo2.FullName);
				string relativePath2 = Path.GetRelativePath(_updateRootInfo.FullName, fileInfo.FullName);
				if (downloadValidity != DownloadValidity.Valid)
				{
					Exception = new InvalidDataException(downloadValidity switch
					{
						DownloadValidity.Missing => "Temporary cache file missing: " + relativePath, 
						DownloadValidity.LengthMismatch => $"Expected {currentFile.Size} bytes but received {fileInfo2.Length}: {relativePath2}", 
						DownloadValidity.ChecksumMismatch => $"Checksum mismatch, expected {currentFile.Checksum} but received {temporaryChecksum}: {relativePath}", 
						DownloadValidity.Valid => throw new UnreachableException(), 
						_ => throw new UnreachableException(), 
					});
					ApplicationContext.CurrentContext.Logger.LogError(Exception, "Error downloading update");
					_failed = true;
					Status = UpdateStatus.Error;
					return false;
				}
				if (IsSelf(currentFile.Path))
				{
					ShouldRestart = true;
					try
					{
						if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						{
							fileInfo2.UnixFileMode |= UnixFileMode.UserExecute;
						}
					}
					catch (Exception exception)
					{
						ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Failed to make " + fileInfo2.Name + " executable");
					}
				}
				try
				{
					if (fileInfo.Exists)
					{
						ApplicationContext.CurrentContext.Logger.LogTrace("Attempting to delete: {TargetFilePath}", relativePath2);
						fileInfo.Delete();
					}
				}
				catch (Exception exception2)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning(exception2, "Failed to delete, falling back to rename {RelativeTargetPath}", relativePath2);
					FileInfo fileInfo3 = new FileInfo(fileInfo.FullName + ".old");
					try
					{
						FileInfo fileInfo4 = new FileInfo(fileInfo.FullName);
						fileInfo4.MoveTo(fileInfo3.FullName, overwrite: true);
					}
					catch (Exception exception3)
					{
						ApplicationContext.CurrentContext.Logger.LogWarning(exception3, "Failed to rename {RelativeSourcePath} to {RelativeTargetPath}.old", relativePath2, relativePath2);
					}
				}
				try
				{
					fileInfo2.MoveTo(fileInfo.FullName, overwrite: true);
					if (IsUpdaterFile(currentFile.Path))
					{
						_updaterContentChanged = true;
					}
					value.State = DownloadState.Completed;
					_downloadedBytes += currentFile.Size;
				}
				catch (Exception exception4)
				{
					ApplicationContext.CurrentContext.Logger.LogTrace(exception4, "Failed to move {SourcePath} to {TargetPath}", fileInfo2.FullName, fileInfo.FullName);
					ApplicationContext.CurrentContext.Logger.LogError(exception4, "Failed to move " + relativePath + " to " + relativePath2);
					try
					{
						using (FileStream fileStream2 = fileInfo2.OpenRead())
						{
							using FileStream destination = fileInfo.OpenWrite();
							fileStream2.CopyTo(destination);
						}
						fileInfo2.Delete();
					}
					catch (Exception exception5)
					{
						ApplicationContext.CurrentContext.Logger.LogError(exception5, "Failed to copy data from " + relativePath + " to " + relativePath2);
					}
				}
			}
			catch (Exception exception6)
			{
				value.State = DownloadState.Failed;
				bool flag = (long)(++value.Essais) < 5L;
				string text = (flag ? string.Empty : "not ");
				string path = currentFile.Path;
				string message = "Error while downloading file, will " + text + " reattempt: " + path;
				if (flag)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning(exception6, message);
					Thread.Sleep(Math.Min(10000, 2000 * value.Essais));
					EnqueueLocked(currentFile);
				}
				else
				{
					ApplicationContext.CurrentContext.Logger.LogError(exception6, message);
				}
			}
			Thread.Yield();
		}
		return true;
	}

	private static int LireOuAbandonner(Stream flux, byte[] tampon, int debut, int taille, CancellationToken annulation)
	{
		using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(annulation);
		cancellationTokenSource.CancelAfter(30000);
		return flux.ReadAsync(tampon.AsMemory(debut, taille), cancellationTokenSource.Token).AsTask().GetAwaiter()
			.GetResult();
	}

	private static DownloadValidity ValidateTemporaryFile(UpdateManifestFile updateManifestFile, FileInfo temporaryFileInfo, out string? temporaryChecksum)
	{
		if (!temporaryFileInfo.Exists)
		{
			temporaryChecksum = null;
			return DownloadValidity.Missing;
		}
		if (temporaryFileInfo.Length != updateManifestFile.Size)
		{
			temporaryChecksum = null;
			return DownloadValidity.LengthMismatch;
		}
		temporaryChecksum = UpdateManifestFile.ComputeChecksum(temporaryFileInfo);
		if (!string.Equals(updateManifestFile.Checksum, temporaryChecksum, StringComparison.Ordinal))
		{
			return DownloadValidity.ChecksumMismatch;
		}
		return DownloadValidity.Valid;
	}

	private bool EstIntact(UpdateManifestFile attendu, FileInfo local)
	{
		if (!local.Exists || local.Length != attendu.Size)
		{
			return false;
		}
		if (_manifesteInstalle != null && _manifesteInstalle.TryGetValue(attendu.Path, out var value) && value.Size == attendu.Size && string.Equals(value.Checksum, attendu.Checksum, StringComparison.Ordinal) && local.LastWriteTimeUtc <= _manifesteInstalleEcrit)
		{
			return true;
		}
		return string.Equals(UpdateManifestFile.ComputeChecksum(local), attendu.Checksum, StringComparison.Ordinal);
	}

	private void EcrireManifesteInstalle(UpdateManifest manifeste)
	{
		try
		{
			File.WriteAllText(_versionPath, JsonConvert.SerializeObject(manifeste, Formatting.None, new JsonSerializerSettings
			{
				DefaultValueHandling = DefaultValueHandling.Ignore
			}));
		}
		catch (Exception exception)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Could not write the installed version manifest");
		}
	}

	private void DeleteTemporaryFiles()
	{
		EnumerationOptions enumerationOptions = new EnumerationOptions
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,
			AttributesToSkip = FileAttributes.None,
			MatchType = MatchType.Win32
		};
		string[] array;
		try
		{
			array = Directory.GetFiles(_updateRootInfo.FullName, "*.old", enumerationOptions).Concat(Directory.GetFiles(_updateRootInfo.FullName, "*.tmp", enumerationOptions)).ToArray();
		}
		catch (Exception exception)
		{
			ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Could not list the leftovers of older updates");
			return;
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			try
			{
				File.Delete(text);
			}
			catch (Exception exception2)
			{
				ApplicationContext.CurrentContext.Logger.LogWarning(exception2, "Exception deleting old file: " + text);
			}
		}
	}

	private static bool IsUpdaterFile(string? path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.StartsWith("resources/updater");
		}
		return false;
	}

	private static bool IsSelf(string updatePath)
	{
		string pathToEntryAssembly;
		return IsSelf(updatePath, out pathToEntryAssembly);
	}

	private static bool IsSelf(string updatePath, [NotNullWhen(true)] out string? pathToEntryAssembly)
	{
		pathToEntryAssembly = Assembly.GetEntryAssembly()?.Location;
		if (string.IsNullOrWhiteSpace(pathToEntryAssembly))
		{
			pathToEntryAssembly = Environment.GetCommandLineArgs().FirstOrDefault();
		}
		if (string.IsNullOrWhiteSpace(pathToEntryAssembly))
		{
			return false;
		}
		string fileName = Path.GetFileName(pathToEntryAssembly);
		return string.Equals(fileName, updatePath, StringComparison.Ordinal);
	}

	private static string ResolvePath(string updatePath, string basePath)
	{
		if (!IsSelf(updatePath, out string pathToEntryAssembly))
		{
			return Path.GetFullPath(updatePath, basePath);
		}
		return pathToEntryAssembly;
	}

	public static string GetHumanReadableFileSize(long size)
	{
		double num = size;
		int num2 = 0;
		while (num > 1024.0 && num2 < SizeSuffixes.Length - 1)
		{
			num2++;
			num /= 1024.0;
		}
		return $"{num:0.##} {SizeSuffixes[num2]}";
	}

	public bool CheckUpdaterContentLoaded()
	{
		if (!_updaterContentChanged)
		{
			return false;
		}
		_updaterContentChanged = false;
		return true;
	}

	public void Stop()
	{
		_stopping = true;
		if (_workerThreads != null)
		{
			Thread[] workerThreads = _workerThreads;
			foreach (Thread thread in workerThreads)
			{
				try
				{
					thread?.Interrupt();
				}
				catch (Exception exception)
				{
					ApplicationContext.CurrentContext.Logger.LogWarning(exception, "Exception while interrupting download thread");
				}
			}
		}
		_stopping = true;
	}
}
