using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.AssetManagement;

public sealed class UpdateManifest
{
	public List<UpdateManifestFile> Files { get; set; } = new List<UpdateManifestFile>();

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? OverrideBaseUrl { get; set; }

	public long StreamingSizeCutoff { get; set; } = 1048576L;

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? StreamingUrl { get; set; }

	public long TotalSize { get; set; }

	public bool TrustCache { get; set; }
}
