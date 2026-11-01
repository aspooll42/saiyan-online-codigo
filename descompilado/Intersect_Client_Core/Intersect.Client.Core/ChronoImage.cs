using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using Intersect.Client.Entities;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Core;

internal static class ChronoImage
{
	public readonly struct Mesure : IDisposable
	{
		private readonly string _nom;

		private readonly double _debut;

		private readonly bool _principale;

		private readonly long _octets;

		public Mesure(string nom, double debut, bool principale)
		{
			_nom = nom;
			_debut = debut;
			_principale = principale;
			_octets = GC.GetAllocatedBytesForCurrentThread();
		}

		public void Dispose()
		{
			Ajouter(_nom, Maintenant - _debut, _principale);
			CompterLesOctets(_nom, _octets, GC.GetAllocatedBytesForCurrentThread());
		}
	}

	private sealed class Ecoute : EventListener
	{
		private readonly object _verrou = new object();

		private readonly Dictionary<string, (int Nombre, double Pause, double Pire)> _passages = new Dictionary<string, (int, double, double)>();

		private readonly Dictionary<string, long> _petits = new Dictionary<string, long>();

		private readonly Dictionary<string, long> _gros = new Dictionary<string, long>();

		private string _passageEnCours = "inconnu";

		private DateTime _suspenduA;

		protected override void OnEventSourceCreated(EventSource source)
		{
			if (source.Name == "Microsoft-Windows-DotNETRuntime")
			{
				EnableEvents(source, EventLevel.Verbose, (EventKeywords)1L);
			}
		}

		protected override void OnEventWritten(EventWrittenEventArgs evenement)
		{
			long value;
			long num2;
			string text;
			string value2;
			string value3;
			switch (evenement.EventId)
			{
			case 1:
				value = Lire(evenement, "Depth");
				num2 = Lire(evenement, "Reason");
				if ((ulong)num2 > 7uL)
				{
					goto IL_00b2;
				}
				switch (num2)
				{
				case 0L:
					break;
				case 1L:
					goto IL_007c;
				case 2L:
					goto IL_0085;
				case 4L:
					goto IL_008e;
				case 5L:
					goto IL_0097;
				case 6L:
					goto IL_00a0;
				case 7L:
					goto IL_00a9;
				default:
					goto IL_00b2;
				}
				text = "petits objets";
				goto IL_00d9;
			case 9:
				lock (_verrou)
				{
					_suspenduA = evenement.TimeStamp;
					break;
				}
			case 3:
				lock (_verrou)
				{
					if (_suspenduA != default(DateTime))
					{
						double totalMilliseconds = (evenement.TimeStamp - _suspenduA).TotalMilliseconds;
						(int, double, double) valueOrDefault = _passages.GetValueOrDefault(_passageEnCours);
						_passages[_passageEnCours] = (valueOrDefault.Item1, valueOrDefault.Item2 + totalMilliseconds, Math.Max(valueOrDefault.Item3, totalMilliseconds));
						_suspenduA = default(DateTime);
					}
					break;
				}
			case 10:
				{
					string key = LireTexte(evenement, "TypeName") ?? "?";
					long num = Lire(evenement, "AllocationAmount64");
					if (num <= 0)
					{
						num = Lire(evenement, "AllocationAmount");
					}
					bool flag = Lire(evenement, "AllocationKind") == 1;
					lock (_verrou)
					{
						Dictionary<string, long> dictionary = (flag ? _gros : _petits);
						dictionary[key] = dictionary.GetValueOrDefault(key) + num;
						break;
					}
				}
				IL_008e:
				text = "gros objets";
				goto IL_00d9;
				IL_0097:
				text = "place (petits)";
				goto IL_00d9;
				IL_007c:
				text = "demande";
				goto IL_00d9;
				IL_0085:
				text = "memoire basse";
				goto IL_00d9;
				IL_00d9:
				value2 = text;
				value3 = Lire(evenement, "Type") switch
				{
					1L => " en arriere-plan", 
					2L => " pendant l'arriere-plan", 
					_ => string.Empty, 
				};
				lock (_verrou)
				{
					_passageEnCours = $"gen{value} {value2}{value3}";
					(int, double, double) valueOrDefault2 = _passages.GetValueOrDefault(_passageEnCours);
					_passages[_passageEnCours] = (valueOrDefault2.Item1 + 1, valueOrDefault2.Item2, valueOrDefault2.Item3);
					break;
				}
				IL_00b2:
				text = $"raison {num2}";
				goto IL_00d9;
				IL_00a9:
				text = "demande douce";
				goto IL_00d9;
				IL_00a0:
				text = "place (gros)";
				goto IL_00d9;
			}
		}

		public (string Passages, string Petits, string Gros) Releve()
		{
			lock (_verrou)
			{
				string item = ((_passages.Count == 0) ? "aucun" : string.Join(", ", from passage in _passages
					orderby passage.Value.Pause descending
					select $"{passage.Value.Nombre} x {passage.Key} ({passage.Value.Pause:0.0} ms, pire {passage.Value.Pire:0.0})"));
				string item2 = Haut(_petits, 8);
				string item3 = Haut(_gros, 5);
				_passages.Clear();
				_petits.Clear();
				_gros.Clear();
				return (Passages: item, Petits: item2, Gros: item3);
			}
		}

		private static string Haut(Dictionary<string, long> table, int combien)
		{
			return string.Join(", ", from type in table.OrderByDescending<KeyValuePair<string, long>, long>((KeyValuePair<string, long> type) => type.Value).Take(combien)
				select $"{type.Key} {(double)type.Value / 1048576.0:0.0} Mo");
		}

		private static long Lire(EventWrittenEventArgs evenement, string nom)
		{
			int num = evenement.PayloadNames?.IndexOf(nom) ?? (-1);
			if (num >= 0)
			{
				ReadOnlyCollection<object> payload = evenement.Payload;
				if (payload != null && num < payload.Count)
				{
					object obj = payload[num];
					if (!(obj is uint num2))
					{
						if (!(obj is int num3))
						{
							if (!(obj is ulong result))
							{
								if (!(obj is long result2))
								{
									if (obj is ushort num4)
									{
										return num4;
									}
									return -1L;
								}
								return result2;
							}
							return (long)result;
						}
						return num3;
					}
					return num2;
				}
			}
			return -1L;
		}

		private static string? LireTexte(EventWrittenEventArgs evenement, string nom)
		{
			int num = evenement.PayloadNames?.IndexOf(nom) ?? (-1);
			if (num >= 0)
			{
				ReadOnlyCollection<object> payload = evenement.Payload;
				if (payload != null && num < payload.Count)
				{
					return payload[num] as string;
				}
			}
			return null;
		}
	}

	private static readonly Stopwatch Montre = Stopwatch.StartNew();

	private static readonly Dictionary<string, double> Etapes = new Dictionary<string, double>(32);

	private static readonly List<string> Notes = new List<string>(8);

	private static double _debutImage = -1.0;

	private static double _periodeHabituelle = 4.0;

	private static double _secondeDesTraces;

	private static int _tracesDeLaSeconde;

	private static readonly int[] RamasseMiettes = new int[3];

	private static readonly StringBuilder Ligne = new StringBuilder(256);

	private static double _principales;

	private static double _dernierTour;

	private static int _dessinsDuDernierTour;

	private static readonly Dictionary<string, int> Dessins = new Dictionary<string, int>(32);

	private static bool _fenetreActivePrecedente;

	private static readonly Dictionary<string, double> Moyennes = new Dictionary<string, double>(32);

	private static readonly Dictionary<string, int> DessinsMoyens = new Dictionary<string, int>(32);

	private static double _debutDuBilan;

	private static double _dureeDuBilan;

	private static double _pireDuBilan;

	private static int _imagesDuBilan;

	private static int _longuesDuBilan;

	private static TimeSpan _pausesAuDebutDuBilan;

	private static TimeSpan _pausesAuDebutImage;

	private const double SeuilDeLenteur = 2.0;

	private static readonly Dictionary<string, long> Octets = new Dictionary<string, long>(32);

	private static readonly Dictionary<string, long> OctetsCumules = new Dictionary<string, long>(32);

	private static long _octetsDuDernierTour;

	private static long _octetsAuDebutImage;

	private static long _octetsDeLImage;

	private static long _octetsDuBilan;

	private static long _tousLesFilsAuDebutDuBilan = -1L;

	private static long _envoisDuBilan;

	private static long _texturesDuBilan;

	private static Ecoute? _ecoute;

	private static int _filDuJeu = -1;

	private static readonly StringBuilder LigneMemoire = new StringBuilder(512);

	public static double Maintenant => Montre.Elapsed.TotalMilliseconds;

	public static long Image { get; private set; }

	internal static int EtapesAuBilan { get; set; } = 10;

	public static Mesure Etape(string nom, bool principale = false)
	{
		return new Mesure(nom, Maintenant, principale);
	}

	public static void Ajouter(string nom, double millisecondes, bool principale = false)
	{
		Etapes[nom] = (Etapes.TryGetValue(nom, out var value) ? (value + millisecondes) : millisecondes);
		if (principale)
		{
			_principales += millisecondes;
		}
	}

	public static void DebutDesTours()
	{
		_dernierTour = Maintenant;
		_dessinsDuDernierTour = Graphics.Renderer?.DessinsDeLImage ?? 0;
		_octetsDuDernierTour = GC.GetAllocatedBytesForCurrentThread();
	}

	public static void Tour(string nom)
	{
		double maintenant = Maintenant;
		Ajouter(nom, maintenant - _dernierTour);
		_dernierTour = maintenant;
		int num = Graphics.Renderer?.DessinsDeLImage ?? 0;
		Dessins[nom] = (Dessins.TryGetValue(nom, out var value) ? (value + num - _dessinsDuDernierTour) : (num - _dessinsDuDernierTour));
		_dessinsDuDernierTour = num;
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		CompterLesOctets(nom, _octetsDuDernierTour, allocatedBytesForCurrentThread);
		_octetsDuDernierTour = allocatedBytesForCurrentThread;
	}

	public static void Noter(string note)
	{
		if (Notes.Count < 8)
		{
			Notes.Add(note);
		}
	}

	public static void NouvelleImage(bool fenetreActive)
	{
		double maintenant = Maintenant;
		Image++;
		TimeSpan totalPauseDuration = GC.GetTotalPauseDuration();
		MesurerLaMemoireDeLImage();
		if (_debutImage >= 0.0 && fenetreActive && _fenetreActivePrecedente)
		{
			double duree = maintenant - _debutImage;
			Juger(duree, (totalPauseDuration - _pausesAuDebutImage).TotalMilliseconds);
			Cumuler(maintenant, duree);
		}
		_pausesAuDebutImage = totalPauseDuration;
		_fenetreActivePrecedente = fenetreActive;
		_debutImage = maintenant;
		Etapes.Clear();
		Dessins.Clear();
		Octets.Clear();
		Notes.Clear();
		_principales = 0.0;
		for (int i = 0; i < RamasseMiettes.Length; i++)
		{
			int num = GC.CollectionCount(i);
			if (RamasseMiettes[i] != 0 && num != RamasseMiettes[i])
			{
				Noter($"ramasse-miettes gen{i}");
			}
			RamasseMiettes[i] = num;
		}
	}

	private static void Cumuler(double maintenant, double duree)
	{
		if (_imagesDuBilan == 0)
		{
			_debutDuBilan = maintenant - duree;
			_pausesAuDebutDuBilan = GC.GetTotalPauseDuration();
		}
		_imagesDuBilan++;
		_dureeDuBilan += duree;
		_pireDuBilan = Math.Max(_pireDuBilan, duree);
		if (duree > Math.Max(_periodeHabituelle * 1.6, _periodeHabituelle + 5.0))
		{
			_longuesDuBilan++;
		}
		string key;
		double value;
		foreach (KeyValuePair<string, double> etape in Etapes)
		{
			etape.Deconstruct(out key, out value);
			string key2 = key;
			double num = value;
			Moyennes[key2] = (Moyennes.TryGetValue(key2, out var value2) ? (value2 + num) : num);
		}
		foreach (KeyValuePair<string, int> dessin in Dessins)
		{
			dessin.Deconstruct(out key, out var value3);
			string key3 = key;
			int num2 = value3;
			DessinsMoyens[key3] = (DessinsMoyens.TryGetValue(key3, out var value4) ? (value4 + num2) : num2);
		}
		CumulerLaMemoire();
		double num3 = maintenant - _debutDuBilan;
		if (num3 < 5000.0 || _imagesDuBilan < 30)
		{
			return;
		}
		Ligne.Clear();
		foreach (KeyValuePair<string, double> item in Moyennes.OrderByDescending<KeyValuePair<string, double>, double>((KeyValuePair<string, double> etape) => etape.Value).Take(EtapesAuBilan))
		{
			item.Deconstruct(out key, out value);
			string text = key;
			double num4 = value;
			double num5 = num4 / (double)_imagesDuBilan;
			if (num5 < 0.05)
			{
				break;
			}
			Ligne.Append((Ligne.Length == 0) ? string.Empty : ", ").Append(text).Append(' ')
				.Append(num5.ToString("0.00"))
				.Append(" ms");
			if (DessinsMoyens.TryGetValue(text, out var value5) && value5 > 0)
			{
				Ligne.Append(" (").Append(value5 / _imagesDuBilan).Append(" dessins)");
			}
		}
		ApplicationContext.Context.Value?.Logger.LogInformation("Bilan des images : {Images} images en {Fenetre:0.0} s, {Moyenne:0.00} ms en moyenne ({Cadence:0} images/s), pire {Pire:0.0} ms, {Longues} longues ; {Lots} lots et {Dessins} dessins par image ; ramasse-miettes {Pauses:0.0} ms ; {Etapes}", _imagesDuBilan, num3 / 1000.0, _dureeDuBilan / (double)_imagesDuBilan, (double)(1000 * _imagesDuBilan) / Math.Max(1.0, num3), _pireDuBilan, _longuesDuBilan, Graphics.Renderer?.LotsDeLImagePrecedente ?? 0, Graphics.Renderer?.DessinsDeLImagePrecedente ?? 0, (GC.GetTotalPauseDuration() - _pausesAuDebutDuBilan).TotalMilliseconds, (Ligne.Length == 0) ? "aucune etape mesuree" : Ligne.ToString());
		EcrireLaMemoire(num3, _imagesDuBilan);
		ConseilDePerformances.Bilan(_dureeDuBilan / (double)_imagesDuBilan);
		Moyennes.Clear();
		DessinsMoyens.Clear();
		_imagesDuBilan = 0;
		_longuesDuBilan = 0;
		_dureeDuBilan = 0.0;
		_pireDuBilan = 0.0;
	}

	private static void Juger(double duree, double pauseGC)
	{
		bool flag = duree > Math.Max(_periodeHabituelle * 1.6, _periodeHabituelle + 5.0);
		_periodeHabituelle += (Math.Min(duree, _periodeHabituelle * 4.0) - _periodeHabituelle) * (flag ? 0.004 : 0.05);
		if (!flag)
		{
			return;
		}
		double num = Math.Floor(Maintenant / 1000.0);
		if (num != _secondeDesTraces)
		{
			_secondeDesTraces = num;
			_tracesDeLaSeconde = 0;
		}
		if (++_tracesDeLaSeconde > 5)
		{
			return;
		}
		Ligne.Clear();
		foreach (var (value, num3) in Etapes.OrderByDescending<KeyValuePair<string, double>, double>((KeyValuePair<string, double> etape) => etape.Value).Take(8))
		{
			if (num3 < 0.3)
			{
				break;
			}
			Ligne.Append((Ligne.Length == 0) ? string.Empty : ", ").Append(value).Append(' ')
				.Append(num3.ToString("0.0"))
				.Append(" ms");
		}
		ApplicationContext.Context.Value?.Logger.LogInformation("Image longue : {Duree:0.0} ms (habituel {Habituel:0.0}) ; {Etapes} ; attente de l'ecran et reste {Reste:0.0} ms ; ramasse-miettes {Pause:0.0} ms ; {Notes}", duree, _periodeHabituelle, (Ligne.Length == 0) ? "aucune etape lourde" : Ligne.ToString(), Math.Max(0.0, duree - _principales), pauseGC, (Notes.Count == 0) ? "rien a signaler" : string.Join(", ", Notes));
	}

	public static void AjouterDepuisLeSon(string nom, double millisecondes)
	{
		if (Environment.CurrentManagedThreadId == _filDuJeu)
		{
			Ajouter(nom, millisecondes);
		}
	}

	public static void CarteLente(string? nom, int couche, double debut)
	{
		double num = Maintenant - debut;
		if (num >= 2.0)
		{
			Noter($"carte {nom} (couche {couche}) {num:0.0} ms");
		}
	}

	public static void EntiteLente(Entity entite, double debut, string quoi)
	{
		double num = Maintenant - debut;
		if (num >= 2.0)
		{
			Noter($"{quoi} {entite.GetType().Name} {entite.Name} {num:0.0} ms");
		}
	}

	private static void CompterLesOctets(string nom, long depuis, long maintenant)
	{
		Octets[nom] = Octets.GetValueOrDefault(nom) + maintenant - depuis;
	}

	private static void MesurerLaMemoireDeLImage()
	{
		if (_filDuJeu < 0)
		{
			_filDuJeu = Environment.CurrentManagedThreadId;
		}
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		_octetsDeLImage = ((_octetsAuDebutImage == 0L) ? 0 : (allocatedBytesForCurrentThread - _octetsAuDebutImage));
		_octetsAuDebutImage = allocatedBytesForCurrentThread;
		try
		{
			if (_ecoute == null)
			{
				_ecoute = new Ecoute();
			}
		}
		catch (Exception exception)
		{
			_ecoute = null;
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Ecoute du ramasse-miettes impossible");
		}
	}

	private static void CumulerLaMemoire()
	{
		if (_tousLesFilsAuDebutDuBilan < 0)
		{
			_tousLesFilsAuDebutDuBilan = GC.GetTotalAllocatedBytes();
		}
		_octetsDuBilan += _octetsDeLImage;
		_envoisDuBilan += Graphics.Renderer?.EnvoisDeLImagePrecedente ?? 0;
		_texturesDuBilan += Graphics.Renderer?.TexturesDeLImagePrecedente ?? 0;
		foreach (var (key, num2) in Octets)
		{
			OctetsCumules[key] = OctetsCumules.GetValueOrDefault(key) + num2;
		}
	}

	private static void EcrireLaMemoire(double fenetre, int images)
	{
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		double num = Math.Max(0.001, fenetre / 1000.0);
		LigneMemoire.Clear();
		foreach (KeyValuePair<string, long> item in OctetsCumules.OrderByDescending<KeyValuePair<string, long>, long>((KeyValuePair<string, long> etape) => etape.Value).Take(EtapesAuBilan))
		{
			item.Deconstruct(out var key, out var value);
			string value2 = key;
			long num2 = value;
			double num3 = (double)num2 / 1024.0 / (double)images;
			if (num3 < 0.1)
			{
				break;
			}
			LigneMemoire.Append((LigneMemoire.Length == 0) ? string.Empty : ", ").Append(value2).Append(' ')
				.Append(num3.ToString("0.0"))
				.Append(" Ko");
		}
		string text = ((LigneMemoire.Length == 0) ? "aucune" : LigneMemoire.ToString());
		var (text2, text3, text4) = _ecoute?.Releve() ?? ("ecoute absente", string.Empty, string.Empty);
		ApplicationContext.Context.Value?.Logger.LogInformation("Memoire des images : fil du jeu {ParImage:0.0} Ko par image ({FilDuJeu:0.0} Mo/s), tous fils {TousFils:0.0} Mo/s ; ramasse-miettes : {Passages} ; etapes (Ko par image) : {Etapes} ; objets crees (echantillons) : {Petits} ; gros objets : {Gros} ; carte graphique : {Envois} envois et {Textures} textures posees par image", (double)_octetsDuBilan / 1024.0 / (double)images, (double)_octetsDuBilan / 1048576.0 / num, (double)(totalAllocatedBytes - _tousLesFilsAuDebutDuBilan) / 1048576.0 / num, text2, text, string.IsNullOrEmpty(text3) ? "aucun" : text3, string.IsNullOrEmpty(text4) ? "aucun" : text4, _envoisDuBilan / images, _texturesDuBilan / images);
		OctetsCumules.Clear();
		_octetsDuBilan = 0L;
		_envoisDuBilan = 0L;
		_texturesDuBilan = 0L;
		_tousLesFilsAuDebutDuBilan = totalAllocatedBytes;
	}
}
