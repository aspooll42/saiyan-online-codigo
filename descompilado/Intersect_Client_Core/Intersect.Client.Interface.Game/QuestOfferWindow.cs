using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.GameObjects;

namespace Intersect.Client.Interface.Game;

public class QuestOfferWindow
{
	private static readonly Color RewardTitleColor = new Color(255, 255, 172, 82);

	private static readonly Color RewardColor = new Color(255, 245, 190, 90);

	private Button mAcceptButton;

	private Button mDeclineButton;

	private string mQuestOfferText = string.Empty;

	private WindowControl mQuestOfferWindow;

	private ScrollControl mQuestPromptArea;

	private RichLabel mQuestPromptLabel;

	private Label mQuestPromptTemplate;

	private Label mQuestTitle;

	public QuestOfferWindow(Canvas gameCanvas)
	{
		mQuestOfferWindow = new WindowControl(gameCanvas, Strings.QuestOffer.Title, modal: false, "QuestOfferWindow");
		mQuestOfferWindow.DisableResizing();
		mQuestOfferWindow.IsClosable = false;
		mQuestTitle = new Label(mQuestOfferWindow, "QuestTitle");
		mQuestPromptArea = new ScrollControl(mQuestOfferWindow, "QuestOfferArea");
		mQuestPromptTemplate = new Label(mQuestPromptArea, "QuestOfferTemplate");
		mQuestPromptLabel = new RichLabel(mQuestPromptArea);
		mAcceptButton = new Button(mQuestOfferWindow, "AcceptButton");
		mAcceptButton.SetText(Strings.QuestOffer.Accept);
		mAcceptButton.Clicked += _acceptButton_Clicked;
		mDeclineButton = new Button(mQuestOfferWindow, "DeclineButton");
		mDeclineButton.SetText(Strings.QuestOffer.Decline);
		mDeclineButton.Clicked += _declineButton_Clicked;
		mQuestOfferWindow.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		Interface.InputBlockingComponents.Add(mQuestOfferWindow);
	}

	private void _declineButton_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.QuestOffers.Count > 0)
		{
			PacketSender.SendDeclineQuest(Globals.QuestOffers[0]);
			Globals.QuestOffers.RemoveAt(0);
		}
	}

	private void _acceptButton_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Globals.QuestOffers.Count > 0)
		{
			PacketSender.SendAcceptQuest(Globals.QuestOffers[0]);
			Globals.QuestOffers.RemoveAt(0);
		}
	}

	public void Update(QuestDescriptor quest)
	{
		if (quest == null)
		{
			Hide();
			return;
		}
		Show();
		mQuestTitle.Text = quest.Name;
		string text = $"{quest.Id}|{quest.StartDescription}";
		if (!(mQuestOfferText != text))
		{
			return;
		}
		mQuestPromptLabel.ClearText();
		mQuestPromptLabel.Width = mQuestPromptArea.Width - mQuestPromptArea.VerticalScrollBar.Width;
		mQuestPromptLabel.AddText(quest.StartDescription, mQuestPromptTemplate);
		List<string> list = QuestRewardText.Parts(quest.Rewards);
		if (list.Count > 0)
		{
			IFont font = mQuestPromptTemplate.Font;
			mQuestPromptLabel.AddText("\n\n" + Strings.QuestOffer.Rewards, RewardTitleColor, Alignments.Left, font);
			foreach (string item in list)
			{
				mQuestPromptLabel.AddText("\n• " + item, RewardColor, Alignments.Left, font);
			}
		}
		mQuestPromptLabel.SizeToChildren(resizeX: false);
		mQuestOfferText = text;
	}

	public void Show()
	{
		mQuestOfferWindow.IsHidden = false;
	}

	public void Close()
	{
		mQuestOfferWindow.Close();
	}

	public bool IsVisible()
	{
		return !mQuestOfferWindow.IsHidden;
	}

	public void Hide()
	{
		mQuestOfferWindow.IsHidden = true;
	}
}
