using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.Localization;

namespace Intersect.Client.Interface.Game.Chat;

public class ChatboxMsg
{
	private static List<ChatboxMsg> sGameMessages = new List<ChatboxMsg>();

	private static Dictionary<ChatboxTab, ChatMessageType[]> sTabMessageTypes = new Dictionary<ChatboxTab, ChatMessageType[]>
	{
		{
			ChatboxTab.Local,
			new ChatMessageType[3]
			{
				ChatMessageType.Local,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.Party,
			new ChatMessageType[3]
			{
				ChatMessageType.Party,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.Global,
			new ChatMessageType[3]
			{
				ChatMessageType.Global,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.World,
			new ChatMessageType[3]
			{
				ChatMessageType.World,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.Guild,
			new ChatMessageType[3]
			{
				ChatMessageType.Guild,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.Trade,
			new ChatMessageType[3]
			{
				ChatMessageType.Trade,
				ChatMessageType.PM,
				ChatMessageType.Admin
			}
		},
		{
			ChatboxTab.System,
			new ChatMessageType[13]
			{
				ChatMessageType.Experience,
				ChatMessageType.Loot,
				ChatMessageType.Inventory,
				ChatMessageType.Bank,
				ChatMessageType.Combat,
				ChatMessageType.Quest,
				ChatMessageType.Crafting,
				ChatMessageType.Trading,
				ChatMessageType.Friend,
				ChatMessageType.Spells,
				ChatMessageType.Notice,
				ChatMessageType.Error,
				ChatMessageType.Admin
			}
		}
	};

	private string mMsg = string.Empty;

	private Color mMsgColor;

	private string mTarget = string.Empty;

	private ChatMessageType mType;

	private const int MemoireDesReprises = 500;

	private bool _choisiALaMain;

	public long ArrivedAtUtc { get; }

	public string Message
	{
		get
		{
			if (!MontreOriginal || Original.Length <= 0)
			{
				return mMsg;
			}
			return Cadre + Original;
		}
	}

	public Color Color => mMsgColor;

	public string Target => mTarget;

	public ChatMessageType Type => mType;

	private static bool TraductionVoulue => Globals.Database?.TraduireLeTchat ?? true;

	public long MessageId { get; }

	public string Langue { get; } = string.Empty;

	public string Original { get; private set; } = string.Empty;

	public EtatDeTraduction Etat { get; private set; }

	public string PhraseId { get; } = string.Empty;

	public string PhraseNom { get; } = string.Empty;

	public int CorpsDebut { get; }

	public bool MontreOriginal { get; private set; }

	public int Revision { get; private set; }

	public static int Changements { get; private set; }

	public bool EstDeJoueur => Langue.Length > 0;

	public bool AUnOriginal
	{
		get
		{
			EtatDeTraduction etat = Etat;
			if (etat - 1 <= EtatDeTraduction.Traduit)
			{
				return Original.Length > 0;
			}
			return false;
		}
	}

	public string Cadre
	{
		get
		{
			if (CorpsDebut <= 0)
			{
				return string.Empty;
			}
			return mMsg.Substring(0, CorpsDebut);
		}
	}

	public ChatboxMsg(string msg, Color clr, ChatMessageType type, string target = "")
	{
		mMsg = msg;
		mMsgColor = clr;
		mTarget = target;
		mType = type;
		ArrivedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}

	public static void AddMessage(ChatboxMsg msg)
	{
		sGameMessages.Add(msg);
	}

	public static List<ChatboxMsg> GetMessages()
	{
		return sGameMessages;
	}

	public static List<ChatboxMsg> GetMessages(ChatboxTab tab)
	{
		List<ChatboxMsg> list = new List<ChatboxMsg>();
		if (tab == ChatboxTab.All)
		{
			list = GetMessages();
		}
		else
		{
			foreach (ChatboxMsg sGameMessage in sGameMessages)
			{
				if (sTabMessageTypes[tab].Contains(sGameMessage.Type))
				{
					list.Add(sGameMessage);
				}
			}
		}
		return list;
	}

	public static void ClearMessages()
	{
		sGameMessages.Clear();
	}

	public ChatboxMsg(string msg, Color clr, ChatMessageType type, string target, long messageId, string? langue, string? original, EtatDeTraduction etat, string? phraseId = null, string? phraseNom = null, int corpsDebut = 0)
		: this(msg ?? string.Empty, clr, type, target ?? string.Empty)
	{
		MessageId = messageId;
		Langue = (langue ?? string.Empty).Trim().ToLowerInvariant();
		Original = original ?? string.Empty;
		Etat = etat;
		PhraseId = phraseId ?? string.Empty;
		PhraseNom = phraseNom ?? string.Empty;
		CorpsDebut = ((corpsDebut > 0 && corpsDebut <= mMsg.Length) ? corpsDebut : 0);
		if (etat == EtatDeTraduction.Phrase)
		{
			EcrireLaPhrase();
		}
		MontreOriginal = !TraductionVoulue && AUnOriginal;
	}

	public static void AppliquerLeReglage()
	{
		bool flag = !TraductionVoulue;
		bool flag2 = false;
		foreach (ChatboxMsg sGameMessage in sGameMessages)
		{
			if (sGameMessage.AUnOriginal && (sGameMessage.MontreOriginal != flag || sGameMessage._choisiALaMain))
			{
				sGameMessage._choisiALaMain = false;
				if (sGameMessage.MontreOriginal != flag)
				{
					sGameMessage.MontreOriginal = flag;
					sGameMessage.Revision++;
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			Changements++;
		}
	}

	private void EcrireLaPhrase()
	{
		if (CorpsDebut <= 0)
		{
			return;
		}
		PhraseDeTchat phraseDeTchat = PhrasesDeTchat.Trouver(PhraseId);
		if ((object)phraseDeTchat == null || !Strings.Tchat.Phrases.TryGetValue(phraseDeTchat.Id, out LocalizedString value))
		{
			return;
		}
		string text;
		if (phraseDeTchat.Trou == TrouDePhrase.Aucun)
		{
			text = value.ToString();
		}
		else
		{
			if (PhraseNom.Length <= 0)
			{
				return;
			}
			text = value.ToString(Traduction.Contenu(PhraseNom));
		}
		mMsg = mMsg.Substring(0, CorpsDebut) + text;
	}

	public bool BasculerOriginal()
	{
		if (!AUnOriginal)
		{
			return false;
		}
		MontreOriginal = !MontreOriginal;
		_choisiALaMain = true;
		Revision++;
		Changements++;
		return true;
	}

	public static bool Reprendre(long messageId, string? texte, EtatDeTraduction etat)
	{
		if (messageId == 0L || string.IsNullOrEmpty(texte))
		{
			return false;
		}
		int num = Math.Max(0, sGameMessages.Count - 500);
		for (int num2 = sGameMessages.Count - 1; num2 >= num; num2--)
		{
			ChatboxMsg chatboxMsg = sGameMessages[num2];
			if (chatboxMsg.MessageId == messageId && chatboxMsg.EstDeJoueur)
			{
				return chatboxMsg.Reprise(texte, etat);
			}
		}
		return false;
	}

	private bool Reprise(string texte, EtatDeTraduction etat)
	{
		if (Etat == EtatDeTraduction.Phrase || etat == EtatDeTraduction.Phrase || CorpsDebut <= 0)
		{
			return false;
		}
		string text = mMsg.Substring(0, CorpsDebut);
		if (etat - 1 <= EtatDeTraduction.Traduit)
		{
			bool flag = Original.Length == 0;
			bool flag2 = flag;
			if (flag2)
			{
				EtatDeTraduction etat2 = Etat;
				bool flag3 = ((etat2 == EtatDeTraduction.Normal || etat2 == EtatDeTraduction.NonTraduit) ? true : false);
				flag2 = flag3;
			}
			if (flag2)
			{
				string text2 = mMsg;
				int corpsDebut = CorpsDebut;
				Original = text2.Substring(corpsDebut, text2.Length - corpsDebut);
			}
		}
		else
		{
			Original = string.Empty;
			MontreOriginal = false;
		}
		mMsg = text + texte;
		Etat = etat;
		if (!_choisiALaMain)
		{
			MontreOriginal = !TraductionVoulue && AUnOriginal;
		}
		Revision++;
		Changements++;
		return true;
	}
}
