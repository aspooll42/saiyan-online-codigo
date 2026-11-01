using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Interface.Shared;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.Security;
using Intersect.Framework.Reflection;
using Intersect.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

public static class Strings
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Friends
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterAll = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterOnline = "Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterOffline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortTool = "Sort";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortHint = "Change the order of the cards";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortName = "Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortLevel = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortSeen = "Last seen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchTool = "Search a friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchPlaceholder = "Name of a friend, a place…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Results = "Results · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoResult = "No friend matches.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingHere = "Nothing here with this filter.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickHint = "Click a friend to see where they are and reach them.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Where = "Where they are";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LastSeenLabel = "Last seen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MessageButton = "Send a message";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteButton = "Invite to the party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveButton = "Remove from friends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddFriend = "Add Friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddFriendPrompt = "Who would you like to add as a friend?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Counts = "{00} online · {01} friends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "You have no friends yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InFight = "You are currently in a fight!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LocationColumn = "LOCATION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Message = "Message";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NameColumn = "NAME";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Party = "Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveFriend = "Remove Friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveFriendPrompt = "Do you wish to remove {00} from your friends list?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Request = "Friend Request";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RequestPrompt = "{00} has sent you a friend request. Do you accept?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionOffline = "OFFLINE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionOnline = "ONLINE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Friends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OnlineChip = "{00} ONLINE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddHint = "Player name to add…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddButton = "ADD";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoneOnline = "No friend online right now.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FooterOne = "1 friend · {00} online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FooterMany = "{00} friends · {01} online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tip = "Tip: right-click a player to add them";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "LV. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenJustNow = "Seen just now";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenMinutes = "Seen {00} min ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenHours = "Seen {00} h ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenYesterday = "Seen yesterday";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenDays = "Seen {00} days ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeenLongAgo = "Not seen for months";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MessageTip = "Send a message";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyTip = "Invite to your party";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Professions
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Blacksmith = "Blacksmith";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cook = "Cook";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Apothecary = "Apothecary";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Engineer = "Engineer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BlacksmithSubtitle = "Gloves, boots, belts and outfits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CookSubtitle = "Dishes that heal and strengthen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApothecarySubtitle = "Vials, antidotes and ki potions";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EngineerSubtitle = "Capsules, weighted gear and gadgets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BlacksmithWorkshop = "Forge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CookWorkshop = "Kitchen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApothecaryWorkshop = "Laboratory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EngineerWorkshop = "Capsule Corp workbench";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreciseChance = "« Precise! » chance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabGathering = "Gathering";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabCrafting = "Crafting";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Recipes = "Recipes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecipesUnlocked = "Recipes unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoRecipe = "No recipe for this profession yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ratio = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Professions";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Woodcutter = "Woodcutter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Miner = "Miner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gatherer = "Gatherer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelRequired = "{00} level {01} needed.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fisher = "Fisher";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hunter = "Hunter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HunterSubtitle = "Combat · animals";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HunterSpeed = "More hunting loot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Game = "Game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GameUnlocked = "Loot unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GameUnlockedCount = "{00} / {01} unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OtherMonsters = "Other monsters: a little experience, no hunting loot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoGame = "No game yet: tick « Game » on a monster in the editor.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WoodcutterSubtitle = "Strength · trees";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MinerSubtitle = "Strength · rocks";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GathererSubtitle = "Speed · plants";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FisherSubtitle = "Speed · water";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WoodcutterSpeed = "Trees felled faster";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MinerSpeed = "Rocks broken faster";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GathererSpeed = "Plants picked faster";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FisherSpeed = "Fish caught faster";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelCaps = "LEVEL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelUp = "LEVEL {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaximumLevel = "Maximum level reached";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceLeft = "{00} more exp for level {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceValue = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Exp = "exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhatYourLevelGives = "What your level gives you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RareChance = "Rare item chance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bonus = "+{00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Resources = "Resources";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnlockedCount = "{00} / {01} unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unlocked = "Unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelsLeft = "{00} more levels";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoResource = "No resource for this profession yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TotalLevel = "TOTAL LEVEL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OutOf = "out of {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RailHint = "Click a card to open its sheet. The wheel scrolls.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickHint = "Click a card to see what it gives, what it needs and where it is made.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterAll = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterUnlocked = "Unlocked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterLocked = "To unlock";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortTool = "Sort";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortHint = "Change the order of the cards";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortLevel = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortName = "Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchTool = "Search";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchPlaceholder = "Name of a resource, a recipe, an item…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Results = "Results · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoResult = "Nothing matches.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingHere = "Nothing here with this filter.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Workshop = "Workshop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Workshops = "Workshops";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HarvestedWith = "Harvested with {00} level {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftedWith = "Crafted by a {00} of level {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftedWithLocked = "Needs {00} level {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HuntedWith = "Hunter level {00} for its loot.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MonsterLevel = "Level {00} monster";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HunterExperience = "+{00} Hunter exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResourceExperience = "+{00} exp per harvest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Drops = "Drops";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HuntingLoot = "Hunting loot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Produces = "Produces";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Workshop
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Crafting";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourBag = "IN YOUR BAG";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WorkshopCaps = "WORKSHOP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InReach = "in reach";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecipesOf = "{00} · {01} / {02} recipes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString All = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Craftable = "Craftable";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecipeCount = "{00} recipes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecipeCountOne = "1 recipe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingCraftable = "Nothing craftable with your bag.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoRecipes = "No recipe at this workshop.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateCraftable = "Craftable ×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateMissingOne = "Missing 1 ingredient";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateMissing = "Missing {00} ingredients";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateLevel = "Level {00} required";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateCrafting = "Crafting…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TimeAndExperience = "{00} s per item · +{01} exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TimeOnly = "{00} s per item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Range = "{00} to {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Heals = "Restores {00} HP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresKi = "Restores {00} Ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bag = "Bag · {00} slots";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ingredients = "INGREDIENTS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagAndNeeded = "BAG / NEEDED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OwnedOfNeeded = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IngredientsCost = "Ingredients cost";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MarketPrice = "Market price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerfectRoll = "perfect roll {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Profit = "Profit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AverageProfit = "Average profit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoListing = "none on sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "Max ×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Craft = "CRAFT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftMany = "CRAFT ×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftingProgress = "CRAFTING  {00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelRequired = "LEVEL {00} REQUIRED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissingIngredients = "MISSING INGREDIENTS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stop = "Stop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Journal = "Recipe book: read only.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct CraftedLoot
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Roll = "ROLL {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Precise = "PRECISE!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreciseRoll = "PRECISE! ROLL {00} %";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Atelier
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RechercheIndice = "Search for a recipe…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FiltreANiveau = "At my level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Emplacements = "Slots";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Categories = "Categories";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Materiaux = "Materials";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Capsules = "Capsules";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Autres = "Other";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneRecette = "Recipe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneNiveau = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneEtat = "Status";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousTitre = "{00} recipes · {01} at your level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousTitreUne = "1 recipe · {00} at your level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Recherche = "Search";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Resultats = "{00} recipes for “{01}”";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultatsUn = "1 recipe for “{00}”";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AucunResultat = "No recipe for “{00}”.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AucuneANiveau = "No recipe at your level here yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedFaisable = "craftable now";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedFaisables = "craftable now";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedPrix = "profit based on auction house prices";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Molette = "wheel: scroll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnCours = "Crafting in progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CliquePourArreter = "click to stop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrixHdv = "Auction house price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MetierSuivant = "Click: next profession ({00} / {01})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DansLeSac = "In your bag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SertA = "Used in {00} recipes here";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SertAUne = "Used in 1 recipe here";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MajDix = "Shift: 10 at a time";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct BienvenueAlpha
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Titre = "Welcome to the alpha";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Point1 = "This is an alpha: the game is still in development, and there will be bugs.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Point2 = "Found a bug? Report it on the Discord, in the “bugs” channel: a screenshot, what you were doing and your character’s name. Every confirmed bug is rewarded.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Point3 = "Progress may be reset at the end of the alpha. Your alpha rewards stay with you: see them in your inventory, Appearance tab.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Point4 = "The server sometimes restarts for updates: follow the “statut-serveur” channel on the Discord.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NePlusAfficher = "Don’t show again";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OuvrirDiscord = "Open Discord";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Jouer = "Let’s go!";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Boutique
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabBuy = "Acheter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabSell = "Vendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Counter = "Le comptoir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SummarySells = "En vente : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SummaryBuys = "achète {00} sortes d'objets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SummaryBuysOne = "achète une sorte d'objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SummaryBuysAll = "achète presque tout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SummaryBuysNothing = "n'achète rien";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YouHave = "Tu en as {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourBag = "Dans ton sac : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotOwned = "Tu n'en as pas encore.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Total = "Total";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YouGet = "Tu reçois";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "Max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Missing = "Il te manque {00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagFull = "Ton sac est plein";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingToSell = "Tu n'as rien que ce marchand achète.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuysNothing = "Ce marchand n'achète rien.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintSell = "Ce qu'il achète est surligné dans ton sac.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Banners
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleTag = "Golden capsule found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleCount = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleGains = "Reward";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleLeft = "{00} more until the \"{01}\" achievement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleUnlocked = "Achievement unlocked · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelUp = "LEVEL UP!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelReached = "Level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementUnlocked = "ACHIEVEMENT UNLOCKED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementDetail = "{00}  -  +{01} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceGained = "+{00} EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceGainedChat = "You gained {00} experience.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TargetPower = "Power {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZonePvp = "PvP zone: other players can attack you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZoneSafe = "Safe zone";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceNew = "New location";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlacePvp = "PvP zone";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlacePvpWarning = "Other players can attack you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceSafe = "Safe zone";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceTime = "Year {00} · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceTag = "Place";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerTag = "Server";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KarmaTag = "Karma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementPoints = "+{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuestGains = "Rewards";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuestGainsOf = "Rewards · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaskDoneTag = "Objective complete";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaskGains = "Step reward";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainZeni = "Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainExp = "EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainMore = "+{00} more";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct WorldMap
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Missing = "La carte du monde n'a pas encore été générée.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Coordinates = "[{00}, {01}]";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Levels = "niveaux {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Dungeon = "DONJON";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DungeonLevel = "Niveau conseillé : {00} à {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DungeonRooms = "{00} salles · difficultés {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Teleport = "TÉLÉPORTATION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TeleportKnown = "Clique sur l'épingle pour la choisir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TeleportLocked = "Pas encore découverte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnnamedMap = "Carte sans nom";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AtSea = "En mer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Monster = "{00} · niv. {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MonsterCount = "×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoMonsters = "Aucun monstre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString You = "Toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unexplored = "Région inexplorée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnknownZone = "Région inconnue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotFromAfterlife = "Pas de voyage depuis le Royaume des morts : parle à Enma pour retourner sur Terre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReturnTitle = "Où veux-tu repartir ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReturnHint = "Choisis un lieu connu, ou ferme la carte pour repartir à ton point de réapparition.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Services = "SERVICES";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Harvest = "RÉCOLTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Objective = "OBJECTIF";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Filters = "Afficher";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterPlaces = "Lieux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterDungeons = "Donjons";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterTeleports = "Téléportations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterObjective = "Objectifs de quête";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterServices = "Services";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterHarvest = "Récolte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Places = "PLACES";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "World Map";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Close = "Close";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LockedMarker = "Undiscovered";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Click a marker to travel there.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoSelection = "No place selected";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TravelButton = "Travel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Travel = "Travel to {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YouAreHere = "You are here: {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Inventaire
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeLabel = "Charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeAlmostFull = "Presque plein";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeOverloaded = "Surchargé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeAmount = "{00} / {01} kg";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeCount = "Charge : {00} / {01} kg";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeOverloadedAlert = "Tu ne peux plus avancer : jette, vends ou range {00} kg.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeTitle = "Ta charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeSummary = "{00} / {01} kg · {02} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeBase = "De base";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeLevel = "Niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeJobs = "Métiers · {00} niveaux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeTotal = "Tu peux porter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kilos = "{00} kg";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KilosMore = "+{00} kg";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeExplained = "Au-delà de 100 %, tu ne peux plus avancer : jette, vends ou range à la banque.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeAlmostExplained = "Encore {00} kg avant d'être surchargé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeOverloadedExplained = "Surchargé de {00} kg : tu ne peux plus avancer tant que tu n'as pas jeté, vendu ou rangé de quoi repasser sous {01} kg.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChargeWornWeighsNothing = "Ce que tu portes sur toi ne pèse rien.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeightLine = "Poids";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeightInBag = "Dans ton sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeightInBagValue = "×{00} · {01} kg";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeightOnceWorn = "Une fois portée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeightWorn = "0 kg · portée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleExtra = "Charge en plus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleActive = "Ta meilleure capsule : tu portes {00} kg de plus.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleOutdone = "Une meilleure capsule compte déjà.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CapsuleOnlyBest = "Seule ta meilleure capsule compte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverloadedMove = "Trop chargé : tu ne peux plus avancer. Jette, vends ou range {00} kg.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabBag = "Sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabStats = "Caractéristiques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabLook = "Apparence";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassAndLevel = "{00} · niveau {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelOnly = "niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CannotWear = "{00} pour porter {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PutOnKey = "{00} posé sur la touche {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PutOnBar = "{00} posé sur la barre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShort = "Niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Trying = "essai";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerScouter = "ce que lit un scouter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerIfEquipped = "si tu l'équipes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerAtLevel = "au niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxHealth = "Vie max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxKi = "Ki max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Niveau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagObject = "objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagObjects = "objets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagFreePlace = "place libre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagFreePlaces = "places libres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDrag = "Glisser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintOr = "ou";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintEquip = "équiper";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CloseSearch = "Fermer la recherche";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointToPlace = "point à placer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsToPlace = "points à placer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsFinal = "réinitialisables contre des Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPointsTitle = "Aucun point à placer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPointsHint = "Tu en gagnes {00} à chaque niveau.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatStepTen = "× 10";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritChance = "Critique";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritDamage = "Dégâts crit.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusFromGear = "Bonus de ton équipement :";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoBonusFromGear = "Ton équipement ne donne aucun bonus en plus.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeOrHold = "Tape un nombre ou maintiens +, puis valide.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointAStat = "Pointe une caractéristique pour voir ce qu'elle t'apporte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PendingOne = "{00} point en attente";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PendingMany = "{00} points en attente";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Annuler";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Validate = "Valider";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResetPoints = "Réinitialiser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResetConfirm = "Confirmer · {00} Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResetHint = "Tous tes points reviennent à placer, contre {00} Zénis.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResetConfirmHint = "Clique encore pour payer {00} Zénis et reprendre tous tes points.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResetNeedZeni = "Il te faut {00} Zénis pour réinitialiser tes points.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotYet = "Pas encore pour toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerExplained = "Tes caractéristiques (×5), ta vie max et ton ki max, multipliés par ton niveau. C'est le chiffre des classements. Sans les transformations.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HealthPoints = "{00} points de vie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KiPoints = "{00} points de ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VitalExplained = "{00} au départ, +{01} par niveau, +{02} par point placé en {03}, plus ton équipement.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelTitle = "Niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceOf = "{00} / {01} d'expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelExplained = "Chaque niveau donne {00} points de caractéristiques, {01} de vie max et {02} de ki max.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatBase = "{00} de base";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatPlacedOne = "{00} placé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatPlacedMany = "{00} placés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatFromGear = "+{00} de ton équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatPending = "+{00} en attente";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusTitle = "Bonus de ton équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusKind = "pièces portées et panoplies";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoBonusExplained = "Aucune des pièces que tu portes ne donne de bonus en plus.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Worn = "porté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShortLower = "niv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unlocked = "Débloqué";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Locked = "Verrouillé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AtLevel = "Au niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClickToWear = "Clic : le porter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WornAlready = "Tu le portes.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotUnlocked = "Pas encore débloqué";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Places = "places";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToolSearch = "Rechercher";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToolSort = "Ranger par type et rareté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchPlaceholder = "Nom d'un objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDoubleClick = "Double-clic";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintUse = "équiper ou utiliser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyShift = "Maj";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintDetails = "détails";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptySlot = "Emplacement vide";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptySlotHint = "Glisse un objet ici, ou double-clique dessus dans le sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SlotFilterHint = "Clic : tes pièces pour cet emplacement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SlotFilterEmpty = "Aucune pièce « {00} » dans ton sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelRequired = "Niveau {00} requis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPieceWith = "Avec cet objet : {00} pièce sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPiecesWith = "Avec cet objet : {00} pièces sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPieceWorn = "Tu portes : {00} pièce sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPiecesWorn = "Tu portes : {00} pièces sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPieceCount = "{00} pièce portée sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPiecesCount = "{00} pièces portées sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetTier = "{00} pièces : {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetMissing = "Il manque : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictBetter = "Meilleur que {00}, que tu portes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictWorse = "Moins bon que {00}, que tu portes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictSame = "Pareil que {00}, que tu portes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictMixed = "{00} en hausse, {01} en baisse, face à {02}, que tu portes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Restores = "Rend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresHealth = "{00} PV";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresKi = "{00} Ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresExperience = "{00} EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresHealthPercent = "{00} % de vie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresKiPercent = "{00} % de ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresExperiencePercent = "{00} % d'expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellsFor = "Se vend {00} Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoSale = "Ne se vend pas";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StackOf = "pile de {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpEquip = "Double-clic : équiper";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpUse = "Double-clic : utiliser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpOpen = "Double-clic : ouvrir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpUnequip = "Double-clic : retirer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpDetails = "Maj : détails";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpDrop = "Glisser hors de la fenêtre : jeter";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Cinematique
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekLegende1 = "Namek s'effondre. Il ne reste que quelques instants.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekLegende2 = "Le choc des deux guerriers fait trembler la planète.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekLegende3 = "Freezer tire sans relâche. Goku esquive.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekLegende4 = "Goku rassemble toute sa puissance…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekLegende5 = "KA… MÉ… HA… MÉ… HA !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekVaincu = "Freezer est vaincu… mais le cœur de Namek va exploser !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekFuite = "Il faut quitter la planète, vite !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekCapsule = "Une capsule de l'équipe Ginyu… allez, démarre !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekDecollage = "Décollage !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamekExplosion = "Namek disparaît dans une gerbe de feu.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Guilds
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MembersLabel = "Members";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MembersCount = "{00} · {01} online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourRank = "Your rank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourClan = "Your clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundNamePlaceholder = "Name of the clan…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InvitePlaceholder = "Name of a warrior to invite…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchPlaceholder = "Name of a clan…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchButton = "Search";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Applications = "Applications ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApplicationsHint = "players asking to join";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApplicationsMore = "… and {00} more, answer these first.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Accept = "Accept";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Refuse = "Refuse";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Apply = "Apply to join";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Applied = "Application sent";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApplyTip = "Ask this clan to let you in: its leaders answer from their clan window.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AppliedHint = "Your application is waiting for an answer. Applying elsewhere replaces it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ApplyHint = "Pick a clan in the directory and apply: no invitation needed.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickMember = "Click a member to see their details and act on them.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickClan = "Click a clan to see its details.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelLabel = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassLabel = "Class";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhereLabel = "Where they are";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaderLabel = "Leader";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundedLabel = "Founded";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TransferButton = "Hand over the clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickButton = "Kick from the clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BrowseHint = "Every clan of the game, the largest first. Click one to see its details.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ViewPage = "See the clan page";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BackToDirectory = "Back to the directory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageLoading = "Loading the clan page…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClanLevel = "Clan level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelChip = "Lv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceBar = "{00} / {01} exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxLevel = "Maximum level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Perks = "+{00} % exp together";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerksTip = "Each level gives +1 % exp to members hunting in a party with a clan mate (+{00} % now) and 5 bank slots (+{01}). Every member pays a tenth of their exp into the clan. This week: {02} exp.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Contribution = "Contribution";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ContributionShort = "{00} exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityLevelUp = "The clan reached level {02}!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectivesTitle = "Week objectives";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectivesEnds = "ends in {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectivesHint = "the whole clan contributes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveDuration = "{00} d {01} h";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveMonsters = "Defeat {00} monsters";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveMonster = "Defeat {00} × {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveQuests = "Complete {00} quests";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveGathering = "Gather {00} times";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveCrafting = "Craft {00} items";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveClanExperience = "Earn {00} clan exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveDone = "Done";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveReward = "+{00} clan exp · {01} zenis for each of the {02} participants";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IdentityButton = "Clan identity";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IdentityTitle = "Clan identity";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IdentityHint = "shown before every member's name and in the directory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagLabel = "Tag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagPlaceholder = "2 to 5 letters";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagHint = "Two to five letters or digits, shown in brackets before each member's name in the world.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmblemLabel = "Emblem";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColorLabel = "Colour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColorNames = "Orange|Red|Blue|Green|Purple|Gold|Cyan|Silver";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Save = "Save";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Cancel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewLabel = "In the world";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageKicker = "Clan · level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageNextLevel = "{00} / {01} exp to level {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageRank = "Ranking";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageWeek = "This week";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageAge = "Clan age";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AgeDays = "{00} d";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AgeHours = "{00} h";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageLeader = "The leader";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageTop = "Most invested";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageTopHint = "exp paid into the clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageNoContribution = "Nobody has paid anything in yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageAlreadyInClan = "You already belong to a clan.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Add = "+";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bank = "{00} Guild Bank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Demote = "Demote to {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DemotePrompt = "Are you sure you want to demote {00} to rank {01}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DemoteTitle = "Demote Guild Member";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Guild = "Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildTip = "Send {00} an invite to your guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Invite = "Invite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteAlreadyInGuild = "The player you're trying to invite is already in a guild or has a pending invite.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteMemberPrompt = "Who would you like to invite to {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteMemberTitle = "Invite Player";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteRequestPrompt = "{00} would like to invite you to join their guild, {01}. Do you accept?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteRequestPromptMissingGuild = "{00} would like to invite you to join their guild. Do you accept?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteRequestTitle = "Guild Invite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kick = "Kick";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickPrompt = "Are you sure you want to kick {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickTitle = "Kick Guild Member";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Leave = "Leave";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeavePrompt = "Are you sure you want to leave your guild?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveTitle = "Leave Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoGuildTitle = "Clans";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "{00} · {01} members";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Members = "Members ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryLeader = "Led by {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryMembers = "{00} members";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryFounded = "Founded {00} ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Currency = "gold";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Found = "Found a guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundButton = "Found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundHint = "Name your guild, 3 to 20 characters.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundCost = "Costs {00} {01}, from level {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundNeedsLevel = "You must reach level {00} to found a guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundNeedsZeni = "You are {00} {01} short of founding a guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundCostFree = "Free, from level {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundTitle = "Found a Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundPrompt = "Founding '{00}' costs {01} {02} and cannot be undone. Go ahead?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Directory = "Guild directory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryButton = "Directory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryBack = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryEmpty = "No guild matches.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryEntry = "{00} - {01} members - led by {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryPage = "{00} guilds - page {01} of {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryPrevious = "<";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryNext = ">";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectoryHint = "Ask a member for an invite to join one.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Motd = "Message of the day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MotdEmpty = "No message of the day yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MotdEdit = "Edit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MotdTitle = "Message of the Day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MotdPrompt = "What should {00}'s message of the day be?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Journal = "Recent activity";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalEmpty = "Nothing has happened yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalEntry = "{00}   {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalNow = "just now";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalMinutes = "{00} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalHours = "{00} h";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JournalDays = "{00} d";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityCreated = "{00} founded the guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityDisbanded = "{00} disbanded the guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityJoined = "{01} joined the guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityLeft = "{01} left the guild.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityKicked = "{00} kicked {01} out.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityPromoted = "{00} promoted {01} to {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityDemoted = "{00} demoted {01} to {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityTransfer = "{00} handed the guild over to {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityRename = "{00} renamed the guild to {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActivityMotd = "{00} rewrote the message of the day.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Disband = "Disband";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisbandTitle = "Disband Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisbandPrompt = "Disbanding {00} cannot be undone: every member loses the guild, and its bank goes with it.\\n\\nIf you really mean it, type '{00}' below.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotAllowedDeposit = "You do not have permission to deposit items into {00}'s guild bank.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotAllowedSwap = "You do not have permission to swap items within {00}'s guild bank.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotAllowedWithdraw = "You do not have permission to withdraw from {00}'s guild bank.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotInGuild = "You are not currently in a guild!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OfflineListEntry = "[{00}] {01} - {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OnlineListEntry = "[{00}] {01} - {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PM = "PM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Promote = "Promote to {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromotePrompt = "Are you sure you want to promote {00} to rank {01}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromoteTitle = "Promote Guild Member";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tooltip = "Lv. {00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Transfer = "Transfer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TransferToMemberPrompt = "This action will completely transfer all ownership of your guild to \\c{{#80ff80}}{00}\\c{{}} and you will lose your rank of \\c{{#ff9f40}}{01}\\c{{}}.\r\n\r\nIf you are sure you want to hand over your guild enter '\\c{{#ff8080}}{02}\\c{{}}' below.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TransferTitle = "Transfer Guild";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Clash
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Debut = "CLASH !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AuBonMoment = "FRAPPE AU BON MOMENT !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Parfait = "PARFAIT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bien = "BIEN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rate = "RATÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Touche = "TOUCHÉ !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Encaisse = "ENCAISSÉ !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Parade = "PARADE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DansLeVide = "DANS LE VIDE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Egalite = "ÉGALITÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Contact = "CONTACT !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Devie = "DÉVIÉ !";
	}

	private class OrdinalComparer : IComparer<string>
	{
		public int Compare(string? x, string? y)
		{
			return string.CompareOrdinal(x, y);
		}
	}

	private class OrdinalComparer<T> : IComparer<T>
	{
		public int Compare(T? x, T? y)
		{
			ref T reference = ref x;
			T val = default(T);
			object strA;
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					strA = null;
					goto IL_0031;
				}
			}
			strA = reference.ToString();
			goto IL_0031;
			IL_0031:
			ref T reference2 = ref y;
			val = default(T);
			object strB;
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
				if (val == null)
				{
					strB = null;
					goto IL_0062;
				}
			}
			strB = reference2.ToString();
			goto IL_0062;
			IL_0062:
			return string.CompareOrdinal((string?)strA, (string?)strB);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct AdminWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipAdmin = "ADMINISTRATOR";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipModerator = "MODERATOR";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Click a player in the world to target them.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionPlayer = "TARGET PLAYER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionActions = "QUICK ACTIONS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionGive = "GIVE AN ITEM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCurrency = "ZENI AND KARMA";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionRewards = "TITLES AND ORNAMENTS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionLook = "APPEARANCE AND ACCESS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionWarp = "TELEPORT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Me = "Me";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MyTarget = "My target";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPlayer = "No player";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotOnScreen = "Not on screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlayerPlaceDistance = "{00}  ·  {01} tiles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionWarpTo = "Go to them";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionSummon = "Summon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionOverworld = "Leave instance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionUnmute = "Unmute";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionUnban = "Unban";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionMute = "Mute";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionKick = "Kick";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionKill = "Kill";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionBan = "Ban";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DangerHint = "In red: a confirmation is asked.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KillPrompt = "The player is knocked out on the spot.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickPrompt = "The player is disconnected from the game.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemSearch = "Search an item…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoItemFound = "No item found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MoreItems = "{00} more: refine your search";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GiveButton = "GIVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionGiveShort = "GIVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Objects = "Items";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Techniques = "Techniques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SpellSearch = "Search a technique…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoSpellFound = "No technique found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TakeButton = "REMOVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Known = "KNOWN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Add = "Add";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Set = "Set";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ok = "OK";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPowerShort = "Set";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MapSearch = "Search a map…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Folders = "Folders";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AToZ = "A-Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoMapFound = "No map found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Here = "HERE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChooseMap = "CHOOSE A MAP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoTo = "GO: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Apply = "Apply";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Give = "Give";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleLabel = "Title";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TakeTitle = "Remove";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrnamentLabel = "Ornament";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TakeOrnament = "Remove";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Item = "Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Zeni = "Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniAdjust = "Adjust by";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniSet = "Set to";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Karma = "Karma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelLabel = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Access = "Access";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassLabel = "Class";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Access1 = "Moderator";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Access2 = "Admin";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ban = "Ban";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BanCaption = "Ban {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BanPrompt = "Banning {00} will not allow them to access this game for the duration you set!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortMapList = "Sort map list";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortMapListTooltip = "Order maps in a flat hierarchy in alphanumeric order according to the computer locale.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Face = "Face";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickPlayer = "Kick";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KillPlayer = "Kill";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MapList = "Map List";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Mute = "Mute";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MuteCaption = "Mute {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MutePrompt = "Muting {00} will not allow them to chat in game for the duration you set!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Name = "Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NamePlaceholder = "Player Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString None = "None";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveInstance = "Leave Instance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetFace = "Set Face";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPower = "Set Power";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetSprite = "Set Sprite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sprite = "Sprite:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Administration";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unban = "Unban";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnbanCaption = "Unban {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnbanPrompt = "Are you sure that you want to unban {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unmute = "Unmute";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnmuteCaption = "Unmute {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnmutePrompt = "Are you sure that you want to unmute {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarpPlayerToMe = "Warp To Me";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarpMeToPlayer = "Warp Me To";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Bags
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RetrieveItem = "Retrieve Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RetrieveItemPrompt = "How many/much {00} would you like to retrieve?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreItem = "Store Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreItemPrompt = "How many/much {00} would you like to store?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Bag";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Bank
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DepositItem = "Deposit Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DepositItemPrompt = "How many/much {00} would you like to deposit?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoSpace = "There is no space left in your bank for that item!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Bank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawItem = "Withdraw Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawItemPrompt = "How many/much {00} would you like to withdraw?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawItemNoSpace = "There is no space left in your inventory for that item!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchHint = "Rechercher";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoMatch = "Aucun objet ne correspond.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourRank = "TON RANG";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RightMove = "Déplacer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RightAllowed = "Autorisé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RanksNobody = "Personne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RanksReserved = "Réservé : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RanksLast = "{00} et {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusalDeposit = "Ton rang ne peut pas déposer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusalWithdraw = "Ton rang ne peut pas retirer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusalMove = "Ton rang ne peut pas déplacer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusalBankFull = "Plus de place dans la banque.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusalBagFull = "Plus de place dans ton sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreNone = "AUCUNE RESSOURCE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreOne = "RANGER 1 RESSOURCE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreMany = "RANGER {00} RESSOURCES";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreTooltip = "Dépose dans la banque tout le minerai, le bois, les poissons… du sac.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct BanMute
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Cancel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Duration = "Duration:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FiveDays = "5 days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FourDays = "4 days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Forever = "Indefinitely";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IncludeIp = "Include IP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IpNo = "No";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IpYes = "Yes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneDay = "1 day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneMonth = "1 month";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneWeek = "1 week";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneYear = "1 year";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Okay = "Okay";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Reason = "Reason:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SixMonths = "6 months";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ThreeDays = "3 days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TwoDays = "2 days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TwoMonths = "2 months";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TwoWeeks = "2 weeks";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Character
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AttackSpeed = "Attack Speed: {00}s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CooldownReduction = "Cooldown Reduction: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Equipment = "Equipment:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExtraBuffDetails = "Details";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExtraExp = "Bonus EXP: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HealthRegen = "Health Regen: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelAndClass = "Level: {00}, Class: {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CombatPower = "Combat Power";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementPoints = "Achievement Points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LifeAndKi = "Life and Ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatsHeading = "Attributes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BaseForm = "Base Form";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bonuses = "Bonuses";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceBonus = "Experience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Lifesteal = "Lifesteal: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Luck = "Instinct: {00}%";

		public static LocalizedString Endurance = "Endurance: {00}%";

		public static LocalizedString Concentration = "Concentration: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ManaRegen = "Mana Regen: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Manasteal = "Manasteal: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Points = "Points: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatLabelValue = "{00}: {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stats = "Stats:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tenacity = "Tenacity: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Character Information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonBalls = "Dragon Balls";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScreenThread = "{00} · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceHeading = "Experience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceMax = "Maximum level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsToSpend = "{00} points to spend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPoints = "no point to spend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepLabel = "Step";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EquipmentShare = "including +{00} from equipment";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Forms = "Forms";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoForms = "No transformation learned yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormActive = "Active";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormsHint = "K: open the techniques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonBallsCount = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonBallOwned = "{00}-star ball: found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonBallMissing = "{00}-star ball: not found yet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonBallsComplete = "All seven gathered. Summon Shenron!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatStepOne = "x 1";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatStepFive = "x 5";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatStepMax = "Max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> StatRoles = new Dictionary<int, LocalizedString>
		{
			{ 0, "Melee blows" },
			{ 1, "Energy techniques" },
			{ 2, "Taking hits" },
			{ 3, "Critical hits" },
			{ 4, "Dodging and tempo" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> StatDescriptions = new Dictionary<int, LocalizedString>
		{
			{ 0, "The raw power of your fists and feet." },
			{ 1, "The energy you gather and release." },
			{ 2, "Reduces all damage you take. Ki gets through: it is only reduced by half." },
			{ 3, "The precision of a seasoned fighter." },
			{ 4, "When you are faster than your opponent, their attacks may only hit an afterimage." }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> StatDetails = new Dictionary<int, LocalizedString>
		{
			{ 0, "Melee damage = +1 / point|Damage beyond 100 = +0.5 / point|Role = Brawler, Tank" },
			{ 1, "Ki damage = +1 / point|Damage beyond 100 = +0.5 / point|Max ki = +2 / point spent|Role = Ki blaster" },
			{ 2, "Max HP = +5 / point spent|Guard = blocks more|Role = Tank" },
			{ 3, "Critical chance = +0.1 % / point|Critical damage = +0.3 % / point|Role = Ki blaster" },
			{ 4, "Dodge = 0.2 % / point ahead|Max dodge = 25 %|Attack tempo = faster|Movement = faster|Role = Brawler" }
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct CharacterCreation
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Class = "Class";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Create = "Create";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Female = "Female";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gender = "Gender";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Customize";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint2 = "Your Character";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InvalidName = "Character name is invalid. Please use alphanumeric characters with a length between 2 and 20.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Male = "Male";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Name = "Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Create a New Character";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct CharacterSelection
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Delete = "Delete";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeletePrompt = "Are you sure you want to delete {00}? This action is irreversible!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteTitle = "Delete {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "Empty Character Slot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Info = "Level: {00}, Class: {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Logout = "Logout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChangePassword = "Change Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Name = "{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString New = "New";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Play = "Play";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Select a Character to Play";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Chatbox
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Channel = "Channel:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fold = "Replier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NewMessages = "{00} nouveaux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabChannel = "Tab change de canal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoldedSpeak = "pour parler ·";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoldedChannel = "change de canal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoldedEnterKey = "Entrée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoldedTabKey = "Tab";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandPrivate = "message privé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandReply = "répondre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandParty = "parler au groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandGuild = "parler au clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandGlobal = "parler à tout le monde";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandTrade = "annoncer une vente";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandLocal = "parler autour de soi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChannelAdmin = "admin";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChannelTrade = "trade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> Channels = new Dictionary<int, LocalizedString>
		{
			{ 0, "local" },
			{ 1, "global" },
			{ 2, "party" },
			{ 3, "guild" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClearLogButtonToolTip = "Clear chat log messages";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<ChatboxTab, LocalizedString> ChatTabButtons = new Dictionary<ChatboxTab, LocalizedString>
		{
			{
				ChatboxTab.All,
				"All"
			},
			{
				ChatboxTab.Local,
				"Local"
			},
			{
				ChatboxTab.Party,
				"Party"
			},
			{
				ChatboxTab.Global,
				"Global"
			},
			{
				ChatboxTab.Guild,
				"Guild"
			},
			{
				ChatboxTab.System,
				"System"
			},
			{
				ChatboxTab.Trade,
				"Trade"
			}
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterChat = "Click here to chat.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterChat1 = "Press '{00}' to chat.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterChat2 = "Press '{00}' or '{01}' to chat.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Send = "Send";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Chat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToggleLogButtonToolTip = "Toggle chat log visibility";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TooFast = "You are chatting too fast!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnableToCopy = "It appears you are not able to copy/paste on this platform. Please make sure you have either the 'xclip' or 'wl-clipboard' packages installed if you are running Linux.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Combat
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AttackWhileCastingDeny = "You cannot attack while casting a spell.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<Stat, LocalizedString> Stats = new Dictionary<Stat, LocalizedString>
		{
			{
				Stat.Attack,
				"Attack"
			},
			{
				Stat.AbilityPower,
				"Ability Power"
			},
			{
				Stat.Defense,
				"Defense"
			},
			{
				Stat.MagicResist,
				"Magic Resist"
			},
			{
				Stat.Speed,
				"Speed"
			}
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarningCharacterSelect = "You are attempting to logout while in combat! Your character will remain in-game until combat has ended. Are you sure you want to logout now?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarningExitDesktop = "You are attempting to exit while in combat! Your character will remain in-game until combat has ended. Are you sure you want to quit now?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarningLogout = "You are attempting to logout while in combat! Your character will remain in-game until combat has ended. Are you sure you want to logout now?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WarningTitle = "Combat Warning!";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Content
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<ContentType, LocalizedString> Types = new Dictionary<ContentType, LocalizedString>
		{
			{
				ContentType.Animation,
				"Animation"
			},
			{
				ContentType.Entity,
				"Entity"
			},
			{
				ContentType.Face,
				"Face"
			},
			{
				ContentType.Fog,
				"Fog"
			},
			{
				ContentType.Font,
				"Font"
			},
			{
				ContentType.Image,
				"Image"
			},
			{
				ContentType.Interface,
				"Interface (gui)"
			},
			{
				ContentType.Item,
				"Item"
			},
			{
				ContentType.Miscellaneous,
				"Miscellaneous"
			},
			{
				ContentType.Music,
				"Music"
			},
			{
				ContentType.Paperdoll,
				"Paperdoll"
			},
			{
				ContentType.Resource,
				"Resource"
			},
			{
				ContentType.Shader,
				"Shader"
			},
			{
				ContentType.Sound,
				"Sound"
			},
			{
				ContentType.Spell,
				"Spell"
			},
			{
				ContentType.TextureAtlas,
				"Texture Atlas"
			},
			{
				ContentType.Tileset,
				"Tileset"
			}
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Controls
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotkeyXLabel = "Hot Key {00}:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> KeyDictionary = new Dictionary<string, LocalizedString>
		{
			{ "attackinteract", "Attack/Interact:" },
			{ "block", "Block:" },
			{ "autotarget", "Auto Target:" },
			{ "enter", "Chat:" },
			{
				"HoldToSoftRetargetOnSelfCast".ToLowerInvariant(),
				"Hold to Soft-Retarget on Self-Cast:"
			},
			{ "hotkey1", "Hot Key 1:" },
			{ "hotkey2", "Hot Key 2:" },
			{ "hotkey3", "Hot Key 3:" },
			{ "hotkey4", "Hot Key 4:" },
			{ "hotkey5", "Hot Key 5:" },
			{ "hotkey6", "Hot Key 6:" },
			{ "hotkey7", "Hot Key 7:" },
			{ "hotkey8", "Hot Key 8:" },
			{ "hotkey9", "Hot Key 9:" },
			{ "hotkey10", "Hot Key 10:" },
			{ "movedown", "Down:" },
			{ "moveleft", "Left:" },
			{ "moveright", "Right:" },
			{ "moveup", "Up:" },
			{ "pickup", "Pick Up:" },
			{ "screenshot", "Screenshot:" },
			{ "openmenu", "Open Menu:" },
			{ "openinventory", "Open Inventory:" },
			{ "openquests", "Open Quests:" },
			{ "opencharacterinfo", "Open Character Info:" },
			{ "openparties", "Open Parties:" },
			{ "openspells", "Open Spells:" },
			{ "openfriends", "Open Friends:" },
			{ "openguild", "Open Guild:" },
			{ "opensettings", "Open Settings:" },
			{ "opendebugger", "Open Debugger:" },
			{ "openadminpanel", "Open Admin Panel:" },
			{ "togglegui", "Toggle Interface:" },
			{ "sprint", "Sprint:" },
			{ "openworldmap", "World Map:" },
			{ "openachievements", "Achievements:" },
			{ "openmarket", "Market:" },
			{ "chargeki", "Charge Ki:" },
			{ "dodge", "Dodge (vanish):" },
			{ "dash", "Dash:" },
			{ "showlocalmap", "Local map (hold):" },
			{ "openprofessions", "Professions:" },
			{ "opencalendar", "Calendar:" },
			{ "openteachings", "Enseignements :" },
			{ "openrankings", "Classements :" },
			{ "turnaround", "Hold to turn around:" },
			{ "togglezoomin", "Toggle Zoom In:" },
			{ "holdtozoomin", "Hold to Zoom In:" },
			{ "togglezoomout", "Toggle Zoom Out:" },
			{ "holdtozoomout", "Hold to Zoom Out:" },
			{ "togglefullscreen", "Toggle Fullscreen:" },
			{
				"ToggleAutoSoftRetargetOnSelfCast".ToLowerInvariant(),
				"Toggle Auto Soft-Retarget on Self-Cast:"
			}
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Listening = "Listening";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Crafting
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Craft = "Craft 1";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftAll = "Craft {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftChance = "Chance of failure: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftingTime = "Crafting time: {00}s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraftStop = "Stop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DestroyMaterialsChance = "Chance to destroy materials: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IncorrectResources = "You do not have the necessary resources to craft this item.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ingredients = "Required:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Product = "Crafting:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Recipes = "Recipes:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecipeListEntry = "{00}) {01}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Credits
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back to Main Menu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Credits";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Debug
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionGPUStatistics = "GPU Statistics";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionGPUAllocations = "GPU Allocations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionSystemStatistics = "System Statistics";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RenderTargetAllocations = "Render Target Allocations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TextureAllocations = "Texture Allocations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TextureCount = "Texture Assets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsedVRAM = "Used VRAM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeVRAM = "Free VRAM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TotalVRAM = "Total VRAM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeVirtualRAM = "Free RAM (Virtual)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TotalVirtualRAM = "Total RAM (Virtual)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreePhysicalRAM = "Free RAM (Physical)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TotalPhysicalRAM = "Total RAM (Physical)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ControlUnderCursor = "Control Under Cursor";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DrawDebugOutlines = "Draw Debug Outlines";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Draws = "Draws";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnableLayoutHotReloading = "Enable Experimental Layout Hot Reloading";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IncludeTextNodesInHover = "Include Text Nodes in Hover";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ViewClickedNodeInDebugger = "View Clicked Node in Debugger";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ViewClickedNodeInDebuggerTooltip = "Hold Alt to temporarily turn this off (so you can turn off this option)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EntitiesDrawn = "Entities Drawn";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fps = "FPS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InterfaceObjects = "Interface Objects";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnownEntities = "Known Entities";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnownMaps = "Known Maps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LightsDrawn = "Lights Drawn";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Map = "Map";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MapsDrawn = "Maps Drawn";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ping = "Ping";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShutdownServer = "Shutdown Server";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShutdownServerAndExit = "Shutdown Server and Exit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormatTextureFromAtlas = "{00} (from atlas)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Time = "Time";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Debug";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleX = "Debug #{0}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabLabelSystem = "System";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabLabelInfo = "Info";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabLabelAssets = "Assets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReloadAsset = "Reload Asset";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AssetsSearchPlaceholder = "Filter assets...";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct EntityBox
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Exp = "EXP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExpValue = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Lv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Map = "{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxLevel = "Max Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NameAndLevel = "{00}    {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital0 = "HP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital0Value = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital1 = "MP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital1Value = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShortVital0 = "HP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShortVital1 = "MP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShortExp = "EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Power = "POWER";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Alerts
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FallbackTitle = "Alert";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<AlertType, LocalizedString> Titles = new Dictionary<AlertType, LocalizedString>
		{
			{
				AlertType.Error,
				"Error"
			},
			{
				AlertType.Warning,
				"Warning"
			}
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Errors
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisconnectionEvent = "Please provide this message to your administrator: {0}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisplayNotSupported = "Invalid Display Configuration!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisplayNotSupportedError = "Fullscreen {00} resolution is not supported on this device!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HostNotFound = "DNS resolution error, please report this to the game administrator.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoadFile = "Failed to load a {00}. Please send the game administrator a copy of your errors log file in the logs directory.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LostConnection = "Lost connection to the game server. Please make sure you're connected to the internet and try again!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotConnected = "Not connected to the game server. Is it online?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InvalidStateReturnToMainMenu = "An invalid state was detected and we had to return to the main menu.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OpenAllLink = "https://goo.gl/Nbx6hx";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OpenGlLink = "https://goo.gl/RSP3ts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PasswordInvalid = "Password is invalid. Please use alphanumeric characters with a length between 4 and 20.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResourcesNotFound = "The resources directory could not be found! Intersect will now close.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Error!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsernameInvalid = "Username is invalid. Please use alphanumeric characters with a length between 2 and 20.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PasswordTooShort = "Choisis un mot de passe d'au moins {00} caractères.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Words
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LcaseSound = "sound";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LcaseSprite = "sprite";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct EventWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Continue = "Continue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintSkip = "{00}: skip the text";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintChoices = "1 to {00}: answer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintContinue = "{00} or Enter: continue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintContinueEnter = "Enter: continue";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ForgotPass
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Disclaimer = "If an account exists with the provided username or email address, an email with a password reset code will be sent to the email address associated with the account.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsernameOrEmailPlaceholder = "Username or Email Address";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Submit = "Submit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Password Reset";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct GameMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Character = "Character Information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Friends = "Friends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Items = "Inventory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Menu = "Open Menu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Party = "Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quest = "Quest Log";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Spells = "Spell Book";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Progression = "Progression";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Social = "Social";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithShortcut = "{00}   ({01})";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct General
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MapItemStackable = "{01} {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString None = "None";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FpsLabelFormat = "{0}fps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PingLabelFormat = "{0}ms";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitTitle = "Quit Game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitPrompt = "Are you sure you want to quit the game?";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Guard
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Perfect = "PERFECT GUARD!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Break = "GUARD BROKEN!";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Vanish
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Evaded = "VANISH!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NouvelleCharge = "+1 CHARGE D'ESQUIVE ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NouvelleChargeDash = "+1 CHARGE DE DASH ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EsquiveDebloquee = "ESQUIVE DÉBLOQUÉE !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EsquiveAuNiveau = "ESQUIVE AU NIVEAU {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DashDebloque = "DASH DÉBLOQUÉ !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DashAuNiveau = "DASH AU NIVEAU {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Combo
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hit = "COMBO x{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Finisher = "FINISHER!";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Scouter
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Label = "POWER LEVEL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Scanning = "SCANNING...";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Danger = "DANGER!";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Market
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Market";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClosedTooFar = "Tu t'es éloigné du commissaire-priseur : l'hôtel des ventes se ferme.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "Buy and sell between warriors";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabBuy = "Buy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabSell = "Sell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabMine = "My sales ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Categories = "Categories";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryAll = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryEquipment = "Equipment";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryConsumables = "Consumables";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryResources = "Resources";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchHint = "Search an item...";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sort = "Sort: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortPriceAscending = "lowest price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortPriceDescending = "highest price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortNewest = "newest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortName = "name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortBestRoll = "best roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Page = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Results = "{00} listing(s) found";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "No listing matches your search.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoCurrencyHint = "The market needs a currency: create an item of type Currency named Zeni in the editor.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SoldBy = "Sold by {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnitPrice = "Lot of {01}: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Buy = "Buy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourListing = "Yours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTitle = "Buy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyQuantityPrompt = "How many {00}? {01} each.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyConfirm = "Buy {00} x {01} for {02}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "Withdraw";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawTitle = "Withdraw";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawConfirm = "Take {00} x {01} off the market?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellHint = "Pick an item in your inventory to put it on sale.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Owned = "You own {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quantity = "Quantity";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnitPriceLabel = "Unit price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellSummary = "Total {00}  ·  tax {01}%: {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellNet = "You receive {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellNetLabel = "You receive";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Total = "Total";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaxLine = "Tax {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaxLabel = "Sale tax";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaxHint = "taken on each sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} listings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PageOf = "Page {00} of {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InventoryHeading = "Your inventory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnItem = "Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnSeller = "Seller";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnState = "Status";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnRoll = "Roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollPercent = "{00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollPerfect = "Perfect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollGood = "Good roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollAverage = "Average";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollWeak = "Weak";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintCheapest = "Cheapest on sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintSameRoll = "Same roll or better";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintNone = "none";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MineHeading = "My sales";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingPending = "nothing to collect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellButton = "Put on sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Selling = "Putting on sale...";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ListingsCount = "Listings: {00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PendingTotal = "to collect: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Pending = "+{00} to collect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CollectAll = "Collect all";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MineEmpty = "You have nothing on sale.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateActive = "{00} left - {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateExpired = "Expired - {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateSold = "Sold out";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TimeDays = "{00}d {01}h";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TimeHours = "{00}h {01}m";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TimeMinutes = "{00} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LockedEquipped = "Unequip this item to sell it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LockedNotTradable = "This item cannot be sold.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultListed = "On sale: {00} x {01} at {02} each.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultBought = "Bought {00} x {01} for {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultCancelled = "Withdrawn from the market: {00} x {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultCollected = "Market: you collected {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultReturned = "Market: {00} expired listing(s) returned to your inventory.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultSold = "Sold at the market: {00} x {01}, you earn {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultProceedsWaiting = "Market: your earnings are waiting, make room in your inventory.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultNothingToCollect = "Nothing to collect.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDisabled = "The market is closed.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultNotEnoughCurrency = "You do not have enough {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInventoryFull = "Your inventory is full.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInvalidItem = "This item is no longer in your inventory.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultTooManyListings = "You reached the maximum number of listings.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInvalidPrice = "Invalid price.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInvalidQuantity = "Invalid quantity.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultListingUnavailable = "This listing is no longer available.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultOwnListing = "You cannot buy your own listing.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultLinkedAccount = "Impossible : ce vendeur joue depuis le même PC que toi.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultAccountTooYoung = "Tu pourras vendre à l'hôtel des ventes au niveau 10.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultError = "Something went wrong, try again.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Karma
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Label = "Karma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hero = "Pure Hero";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Neutral = "Neutral";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Suspect = "Suspect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Criminal = "Criminal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Aggressor = "AGGRESSOR";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BecameTier = "YOU ARE NOW {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BecameNeutral = "YOU ARE NEUTRAL AGAIN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BannerValue = "Karma: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TargetNameAndTier = "{00}  ·  {01}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Hell
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sentence = "HELL · SENTENCE {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bail = "Serve your sentence to wipe your sins, or pay {00} Zeni at the counter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WindowTitle = "Leave Hell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CounterName = "Hell's counter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CounterHint = "You leave at once, but as a Suspect.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SentenceLeft = "Sentence left";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BailCaption = "Bail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BalanceAfter = "Your Zeni after paying";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Missing = "You are short";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Wait = "Wait";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Pay = "Pay the bail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrisonSentence = "PRISON · SENTENCE {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrisonBail = "Serve your sentence to wipe your crimes, or pay {00} Zeni at the counter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrisonWindowTitle = "Leave prison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrisonCounterName = "Prison counter";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TrackingRadar
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Tracking Radar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stock = "Single use · {00} radars in your bag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StockOne = "Single use · 1 radar in your bag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Wanted = "Wanted Criminals";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoneWanted = "No Criminal is wanted right now.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OfflineSince = "Offline for {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Locate = "Locate";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Searching = "Searching…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Target = "Target: {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Warned = "They will be warned, never by whom.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TargetOffline = "{00} is not connected.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kept = "Your radar is kept.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchingSignal = "Searching for the signal…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Found = "{00} is on the map {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Used = "Radar used.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Jammed = "Signal not found.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JammedScreen = "SIGNAL NOT FOUND";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WentOffline = "{00} just left.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotWanted = "{00} is no longer wanted.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoRadar = "No radar left in your bag.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotAllowed = "The radar only answers honest hands.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Page = "{00}/{01}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct WantedBoard
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Wanted";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StationIn = "Police station · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Station = "Police station";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabCriminals = "Criminals  {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabPolice = "Police bounties  {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabGrades = "Grades";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabRules = "Rules";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Reputation = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextGrade = "{00} at {01} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TopGrade = "The highest grade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Wanted = "Wanted";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Criminal = "Criminal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PoliceBounty = "Police bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExpiresIn = "expires in {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassLevel = "{00} · level {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EliteLevel = "Elite target · level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OfflineSince = "Offline for {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BountyUnit = "Zeni bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Reward = "{00} Z · {01} XP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LastSeen = "Last seen: {00}, {01} ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NeverSeen = "Last seen: unknown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnknownPlace = "somewhere";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneMurder = "Crimes: 1 murder";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Murders = "Crimes: {00} murders";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoHunter = "Nobody on their trail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneHunter = "1 hunter on their trail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hunters = "{00} hunters on their trail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelGap = "{00} levels above them: {01} Z for you, no reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Spotted = "Spotted: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PaidByPolice = "Paid by the police";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DoneToday = "Completed today";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Take = "Take the contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Taken = "Contract taken";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Abandon = "Drop the contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Done = "Completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Full = "Contracts full";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HonestOnly = "Honest hands only";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoCriminal = "No Criminal is wanted right now.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoBounty = "No police bounty for you today.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DrawnAtLevel = "Drawn at your level ({00}): targets from {01} to {02}, paid by their level.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ContractsHeld = "Contracts: {00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RadarHint = "A Criminal's exact map: Tracking Radar, at the merchants.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString You = "YOU";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade0 = "Informant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade1 = "Auxiliary";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade2 = "Officer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade3 = "Inspector";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade4 = "Commissioner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade5 = "Justicar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeBounties = "{00} police bounties a day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeRadar = "the Tracking Radar at -{00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeElite = "better paid elite targets (+{00}%)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeRespect = "The most respected grade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainsTitle = "What raises your police reputation:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainBounty = "+{00}  a police bounty completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainCriminal = "+{00}  a Criminal taken down, shared like their bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GainsNote = "Becoming a Suspect freezes your reputation; becoming a Criminal resets it to zero, and takes your police titles.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeTitle = "“{00}”";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesCriminals = "Criminals";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleRace = "Several hunters can target the same Criminal: the first to take them down wins the bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleShare = "Hit by several hunters in the last {00} seconds: the bounty is shared by damage.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleZone = "The board gives the zone where they were seen; the Tracking Radar gives the exact map.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleNobody = "Arrested by the police, or killed by a monster: nobody earns the bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleCloses = "Your contract closes by itself if they are taken down, no longer wanted, or after {00} h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleGap = "{00} levels above them: {01}% of the bounty, no reputation.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesPolice = "Police bounties";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleDaily = "{00} elite targets a day, drawn at your level; up to {01} with the grades.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleOnce = "Each bounty can be completed once a day by each player.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleTogether = "Together, you are paid if you hold the contract and did at least {00}% of the damage. A group's damage counts together.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleNoContract = "Without a contract, you keep the loot and experience, but not the bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesLogout = "Logging out";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleCombat = "In the middle of a fight, your character stays in game until it ends.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleOutlaw = "Suspect or Criminal: your character stays {00} s where you left it, except in a hideout.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleChase = "Chased by the police, you are in combat: quitting the game won't save you.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleOffline = "« Offline » on a poster: the Criminal is not connected. You can still take the contract, it runs {00} h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesGrades = "The grades";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleRise = "Completed bounties and Criminals taken down raise your reputation.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleGradeGives = "Each grade gives more bounties, a cheaper Radar or better paid targets, and a title.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleReset = "Becoming a Criminal resets your police reputation to zero.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesGood = "Good to know";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleContracts = "{00} contracts at once, Criminals and police together.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleHour = "The same target only pays you once an hour.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerCriminal = "Contract: {00} (Criminal)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerPolice = "Contract: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerBounty = "Bounty: {00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerReward = "Reward: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerCompleted = "To cash in: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerGoBack = "Go back to a wanted board";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerCompletedReward = "Bounty: {00} · +{01} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToCashIn = "To cash in";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FulfilledLine = "Fulfilled: cash it in at the board";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct WantedTurnIn
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleOne = "Contract fulfilled";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleMany = "Contracts fulfilled";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Killed = "Taken down: {00}, {01} ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Total = "Total";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Wallet = "Your purse";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Later = "Later";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CashIn = "Cash in";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CashInAll = "Cash in all";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cashed = "Cashed in";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SeeBoard = "See the posters";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReputationGain = "+{00} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReputationFrozen = "reputation frozen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Promotion = "Promotion · {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct RubanRouge
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Red Ribbon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Trafficker = "The trafficker";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TraffickerIn = "The trafficker · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabContracts = "Contracts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabMarket = "Black market";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabGrades = "Grades";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabRules = "Rules";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoContractYet = "No contract yet: the trafficker will have work soon.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Reputation = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextGrade = "{00} at {01} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TopGrade = "The most feared grade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade0 = "Recruit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade1 = "Soldier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade2 = "Sergeant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade3 = "Lieutenant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade4 = "Colonel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grade5 = "General";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens0 = "Smuggling · the Smoke bomb";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens1 = "Sabotaging a wooden house";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens2 = "Sabotaging a town building · the Disguise";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens3 = "A Hero's head · the Jammer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens4 = "Group contracts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Opens5 = "The most feared grade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString You = "You";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Purse = "In your purse:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniShort = "{00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unit = "Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SmokeBombEffect = "Escape once from a police officer who catches you.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisguiseEffect = "Your name shows in white: the police do not recognise you, as long as you hit nobody.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString JammerEffect = "Invisible to the Tracking Radar: the hunter reads \"Signal not found\" and keeps his radar.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsedUp = "Used up when it triggers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Minutes = "{00} minutes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Carried = "In your bag: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Buy = "Buy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeRequired = "{00} required";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoZeni = "Not enough Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unavailable = "Out of stock";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bounty = "Your bounty: {00} Z locked, for the hunter who takes you down.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoBounty = "No bounty on your head.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesContracts = "The contracts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleOutlaws = "Only Suspects and Criminals find work here.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleDaily = "{00} contract at a time, {01} a day, drawn at your level: the pay follows.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleSabotage = "Sabotage: the building must fall by your hand or your group's.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleSmuggling = "Smuggling: no flying and no teleporting while you carry the goods.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleHero = "A Hero's head: a Hero within {00} levels of you; he is warned, never by whom.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesPay = "The pay";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleZeniOnly = "Zeni and reputation, never experience nor items.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleMore = "It pays more than the police bounties.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleShare = "But with each crime, part of your Zeni goes into your bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleSpend = "Your Zeni buy from the black market: Smoke bomb, Disguise, Jammer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesCaught = "Getting caught";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleHunted = "Taken down by a hunter: contract lost, and he collects your bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleArrested = "Arrested by the police: contract lost, bounty seized, -{00} reputation.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleSeized = "The smuggled goods are seized: whoever took you down gets a reward.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RulesGrades = "The grades";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleRise = "Reputation raises your grade, from Recruit to General.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleGradeGives = "Each grade opens new contracts, black market articles and a title.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabContractsCount = "Contracts {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KindSmuggling = "Smuggling";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KindHead = "Hero's head";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KindSabotage = "Sabotage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeTag = "Grade {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SmugglingWho = "For the fence of {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadWho = "Hero · level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RepChip = "+{00} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SmugglingArrival = "Arrival: {00}, the fence is waiting";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SmugglingNoFly = "No flying and no teleporting with the goods";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SmugglingSeized = "Killed or arrested: the goods are seized";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadSeenNow = "Seen just now: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadSeen = "Last known place: {00}, {01} ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadDownNone = "Took down no Criminal this week";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadDownOne = "Took down 1 Criminal this week";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadDownMany = "Took down {00} Criminals this week";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadGap = "You, level {00}: within {01} levels";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadWarned = "He is warned, never of your name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeadOffline = "Offline: wait for his return";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Take = "Take the contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Taken = "Contract taken";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Abandon = "Abandon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneAtATime = "One contract at a time";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradeLocked = "{00} required";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DoneToday = "Done today";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoMoreToday = "No more today";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FooterCount = "Contract in progress: {00} / {01} · today: {02} / {03}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerSmuggling = "Smuggling: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerHead = "Hero's head: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerDeliver = "Take the goods to the fence of {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackerPay = "Pay: {00} Z · +{01} reputation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GradesNote = "Each grade gives its title, to show under your name like the other titles of the game.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Calendar
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Calendar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Age = "Age {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Spring = "Spring";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Summer = "Summer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Autumn = "Autumn";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Winter = "Winter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TurtleMonth = "Month of the Turtle";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CraneMonth = "Month of the Crane";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CloudMonth = "Month of the Cloud";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragonMonth = "Month of the Dragon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TournamentMonth = "Month of the Tournament";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SunMonth = "Month of the Sun";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MonkeyMonth = "Month of the Monkey";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MoonMonth = "Month of the Moon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SnakeMonth = "Month of the Snake";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WolfMonth = "Month of the Wolf";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SnowMonth = "Month of the Snow";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnmaMonth = "Month of Enma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FirstMonthRank = "1st month · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MonthRank = "Month {00} · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OneStar = "Day 1 star";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stars = "Day {00} stars";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FirstOfMonth = "1 {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DayOfMonth = "{00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MonthYear = "{00} · Age {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Today = "Today";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Upcoming = "Coming up";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FullMoon = "Full moon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Budokai = "Tenkaichi Budokai";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tonight = "tonight";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TodayLower = "today";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tomorrow = "tomorrow";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InDays = "in {00} days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealOneMinute = "{00} real minute";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealMinutes = "{00} real minutes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealOneHour = "{00} real hour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealHours = "{00} real hours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealOneDay = "{00} real day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RealDays = "{00} real days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Dawn = "Dawn";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Daylight = "Full daylight";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sunset = "Sunset";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Night = "Night";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextStep = "{00} in {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextWeather = "{00}, then {01} in {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ThenWeather = "Then {00} in {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShortMinutes = "{00} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShortHours = "{00} h {01:00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rhythm1 = "A game hour lasts {00}, a day {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rhythm2 = "A month lasts {00}, a year {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationMinutes = "{00} minutes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationOneHour = "1 hour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationHours = "{00} hours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationDays = "{00} days";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationDaysHours = "{00} days and {01} hours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationWeeks = "{00} weeks";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClockDate = "{00}, {01} · Age {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClockClick = "Click: the calendar ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlayersOnline = "players online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlayerOnline = "player online";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Achievements
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Achievements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllCategories = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsSummary = "{00} / {01} points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Progress = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Points = "{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Completed = "Completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "No achievements yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unlocked = "Achievement unlocked: {00} (+{01} pts)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "Your journey, milestone by milestone";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabLabel = "{00}  {01}/{02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rewards = "REWARDS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardExperience = "+{00} EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardItem = "{00} x{01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardZeni = "{00} Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardTitle = "\"{00}\"";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoReward = "-";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Obtained = "OBTAINED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsShort = "PTS";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct DailyRewards
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Daily Rewards";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "One square per day you show up";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Total = "{00} days claimed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Day = "Day {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Claim = "CLAIM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ready = "Today's square is waiting for you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ComeBack = "Come back tomorrow for the next square";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "No rewards set up.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Granted = "Day {00} reward: {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Today = "DAY {00}  ·  TODAY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tomorrow = "DAY {00}  ·  TOMORROW";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Streak = "STREAK";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CycleProgress = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextMilestone = "Next milestone in {00} days: {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextMilestoneTomorrow = "Next milestone tomorrow: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MilestoneToday = "Milestone! {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniUnit = "Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceUnit = "XP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusValue = "+{00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusUnit = "XP · {00} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniExtra = "+{00} Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemQuantity = "{00} x{01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Claimed = "Claimed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ComeBackShort = "Come back tomorrow";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPenalty = "Missing a day costs nothing: you pick up where you left off.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct LocalMap
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZoneSafe = "SAFE ZONE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZonePvp = "PVP OPEN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Release = "Release";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToClose = "to close";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LegendYou = "you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LegendNpc = "NPC";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LegendTeammate = "teammate";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LegendExit = "way to the next map";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct InputBox
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Cancel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString No = "No";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Okay = "Okay";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Yes = "Yes";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct MapItemWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LootButton = "Loot All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Loot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickUpHint = "pick up";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Internals
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bounds = "Bounds";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Color = "Color";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColorOverride = "Color Override";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoordinateX = "X";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoordinateY = "Y";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoordinateZ = "Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperimentalFeatureTooltip = "This feature is experimental and may cause issues when enabled.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GlobalItem = "Global {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LocalItem = "Local {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Name = "Name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotApplicable = "N/A";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResolutionXByY = "{0}x{1}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Type = "Type";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InnerBounds = "Inner Bounds";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Margin = "Margin";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Padding = "Padding";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Dock = "Dock";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Alignment = "Alignment";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TextAlign = "Text Align";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OuterBounds = "Outer Bounds";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Font = "Font";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSizeToContents = "AutoSizeToContents";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FontSize = "Font Size";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSizeToContentWidth = "AutoSizeToContentWidth";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSizeToContentHeight = "AutoSizeToContentHeight";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSizeToContentWidthOnChildResize = "ASTW on Child Resize";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSizeToContentHeightOnChildResize = "ASTH on Child Resize";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IsVisibleInParent = "IsVisibleInParent";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IsVisibleInTree = "IsVisibleInTree";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IsDisabled = "IsDisabled";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IsDisabledByTree = "IsDisabledByTree";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Inventory
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropItemPrompt = "How many units of {00} would you like to discard?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropItemTitle = "Drop Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropPrompt = "Do you wish to drop the item: {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EquippedSymbol = "E";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Inventory";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryAll = "Tous les objets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryQuest = "Objets de quête";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllCategories = "Toutes catégories";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllSlots = "Tous les emplacements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchHint = "Recherche";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CountNone = "Aucun objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CountOne = "1 objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CustomizationHint = "Survole un nom pour l'essayer, clique pour le porter";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct EntityContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddFriend = "Add Friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteToGuild = "Invite to Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteToParty = "Invite to Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrivateMessage = "Private Message";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Trade = "Trade";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct DropdownMenus
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlreadyFriend = "already a friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Available = "Available";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HasGuild = "has a guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InCombat = "in combat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourParty = "in your party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelAndKarma = "Lv. {00}  ·  {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShort = "Lv {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "{00} online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyFull = "party full";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ItemContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bag = "Bag {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bank = "Bank {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cast = "Cast {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Drop = "Drop {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Equip = "Equip {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Learn = "Learn {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Open = "Open {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sell = "Sell {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Trade = "Offer {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unequip = "Unequip {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Use = "Use {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct SpellContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cast = "Cast {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Forget = "Forget {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct BankContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "Withdraw {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct BagContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "Withdraw {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TradeContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "Revoke {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ChatContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FriendInvite = "Send Friend request to {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildInvite = "Invite {00} to Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyInvite = "Invite {00} to Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PM = "Send Private Message to {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ShopContextMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Buy = "Buy {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ItemDescription
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Amount = "Amount:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AttackSpeed = "Attack Speed:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagSlots = "Bag Slots:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bagged = "Bagged";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Banked = "Banked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BaseDamage = "Base Damage:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BaseDamageType = "Damage Type:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BlockAbsorption = "Block Absorption:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BlockAmount = "Block Amount:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BlockChance = "Block Chance:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CastSpell = "Casts Spell: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritChance = "Critical Chance:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritMultiplier = "Critical Multiplier:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Description = "{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropOnDeath = "Drop chance on death:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EstimatedValue = "Estimated value:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassificationSeparator = "   •   ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RarityRange = "{00} à {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetPieces = "{00}/{01} pièces";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetBonusPieces = "{00} pièces :";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetBonusActive = "{00} pièces · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Dropped = "Dropped";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildBanked = "Guild Banked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemLimits = "Cannot be {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Multiplier = "{00}x";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Percentage = "{00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegularAndPercentage = "{00} + {01}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScalingPercentage = "Scaling Percentage:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScalingStat = "Scaling Stat:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SingleUse = "Single use";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sold = "Sold";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatGrowthRange = "{00} to {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RolledRange = "{00} to {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RolledStat = "{00} ({01}):";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Roll = "Roll:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollValue = "{00}% · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollPerfect = "Perfect roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollGood = "Good roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollAverage = "Average roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RollWeak = "Weak roll";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TeachSpell = "Teaches Spell: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Traded = "Traded";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TwoHand = "2H";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> BonusEffects = new Dictionary<int, LocalizedString>
		{
			{ 0, "" },
			{ 1, "Cooldown Reduction:" },
			{ 2, "Lifesteal:" },
			{ 3, "Tenacity:" },
			{ 4, "Instinct:" },
			{ 5, "Bonus Experience:" },
			{ 6, "Manasteal:" },
			{ 7, "Endurance:" },
			{ 8, "Concentration:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> ConsumableTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "Restores HP:" },
			{ 1, "Restores MP:" },
			{ 2, "Grants Experience:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> DamageTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "Physical" },
			{ 1, "Magic" },
			{ 2, "True" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> ItemTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "None" },
			{ 1, "Equipment" },
			{ 2, "Consumable" },
			{ 3, "Currency" },
			{ 4, "Spell" },
			{ 5, "Special" },
			{ 6, "Bag" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> StatCounts = new Dictionary<int, LocalizedString>
		{
			{ 0, "Attack:" },
			{ 1, "Ability Power:" },
			{ 2, "Defense:" },
			{ 3, "Magic Resist:" },
			{ 4, "Speed:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> Stats = new Dictionary<int, LocalizedString>
		{
			{ 0, "Attack" },
			{ 1, "Ability Power" },
			{ 2, "Defense" },
			{ 3, "Magic Resist" },
			{ 4, "Speed" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> Vitals = new Dictionary<int, LocalizedString>
		{
			{ 0, "HP:" },
			{ 1, "MP:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> VitalsRegen = new Dictionary<int, LocalizedString>
		{
			{ 0, "HP Regen:" },
			{ 1, "MP Regen:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> Rarity = new Dictionary<string, LocalizedString>
		{
			{ "None", "None" },
			{ "Common", "Common" },
			{ "Uncommon", "Uncommon" },
			{ "Rare", "Rare" },
			{ "Epic", "Epic" },
			{ "Legendary", "Legendary" }
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Keys
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> KeyDictionary = new Dictionary<string, LocalizedString>
		{
			{ "a", "A" },
			{ "add", "Add" },
			{ "alt", "Alt" },
			{ "apps", "Apps" },
			{ "attn", "Attn" },
			{ "b", "B" },
			{ "back", "Back" },
			{ "browserback", "BrowserBack" },
			{ "browserfavorites", "BrowserFavorites" },
			{ "browserforward", "BrowserForward" },
			{ "browserhome", "BrowserHome" },
			{ "browserrefresh", "BrowserRefresh" },
			{ "browsersearch", "BrowserSearch" },
			{ "browserstop", "BrowserStop" },
			{ "c", "C" },
			{ "cancel", "Cancel" },
			{ "capital", "Capital" },
			{ "capslock", "CapsLock" },
			{ "clear", "Clear" },
			{ "control", "Control" },
			{ "controlkey", "ControlKey" },
			{ "crsel", "Crsel" },
			{ "d", "D" },
			{ "d0", "0" },
			{ "d1", "1" },
			{ "d2", "2" },
			{ "d3", "3" },
			{ "d4", "4" },
			{ "d5", "5" },
			{ "d6", "6" },
			{ "d7", "7" },
			{ "d8", "8" },
			{ "d9", "9" },
			{ "decimal", "Decimal" },
			{ "delete", "Delete" },
			{ "divide", "Divide" },
			{ "down", "Down" },
			{ "e", "E" },
			{ "end", "End" },
			{ "enter", "Enter" },
			{ "eraseeof", "EraseEof" },
			{ "escape", "Escape" },
			{ "execute", "Execute" },
			{ "exsel", "Exsel" },
			{ "f", "F" },
			{ "f1", "F1" },
			{ "f10", "F10" },
			{ "f11", "F11" },
			{ "f12", "F12" },
			{ "f13", "F13" },
			{ "f14", "F14" },
			{ "f15", "F15" },
			{ "f16", "F16" },
			{ "f17", "F17" },
			{ "f18", "F18" },
			{ "f19", "F19" },
			{ "f2", "F2" },
			{ "f20", "F20" },
			{ "f21", "F21" },
			{ "f22", "F22" },
			{ "f23", "F23" },
			{ "f24", "F24" },
			{ "f3", "F3" },
			{ "f4", "F4" },
			{ "f5", "F5" },
			{ "f6", "F6" },
			{ "f7", "F7" },
			{ "f8", "F8" },
			{ "f9", "F9" },
			{ "finalmode", "FinalMode" },
			{ "g", "G" },
			{ "h", "H" },
			{ "hanguelmode", "HanguelMode" },
			{ "hangulmode", "HangulMode" },
			{ "hanjamode", "HanjaMode" },
			{ "help", "Help" },
			{ "home", "Home" },
			{ "i", "I" },
			{ "imeaccept", "IMEAccept" },
			{ "imeaceept", "IMEAceept" },
			{ "imeconvert", "IMEConvert" },
			{ "imemodechange", "IMEModeChange" },
			{ "imenonconvert", "IMENonconvert" },
			{ "insert", "Insert" },
			{ "j", "J" },
			{ "junjamode", "JunjaMode" },
			{ "k", "K" },
			{ "kanamode", "KanaMode" },
			{ "kanjimode", "KanjiMode" },
			{ "keycode", "KeyCode" },
			{ "l", "L" },
			{ "launchapplication1", "LaunchApplication1" },
			{ "launchapplication2", "LaunchApplication2" },
			{ "launchmail", "LaunchMail" },
			{ "lbutton", "Left Mouse" },
			{ "lcontrolkey", "L Control" },
			{ "left", "Left" },
			{ "linefeed", "LineFeed" },
			{ "lmenu", "L Alt" },
			{ "lshiftkey", "L Shift" },
			{ "lwin", "LWin" },
			{ "m", "M" },
			{ "mbutton", "Middle Mouse" },
			{ "medianexttrack", "MediaNextTrack" },
			{ "mediaplaypause", "MediaPlayPause" },
			{ "mediaprevioustrack", "MediaPreviousTrack" },
			{ "mediastop", "MediaStop" },
			{ "menu", "Menu" },
			{ "modifiers", "Modifiers" },
			{ "multiply", "Multiply" },
			{ "n", "N" },
			{ "next", "Next" },
			{ "noname", "NoName" },
			{ "none", "None" },
			{ "numlock", "NumLock" },
			{ "numpad0", "NumPad 0" },
			{ "numpad1", "NumPad 1" },
			{ "numpad2", "NumPad 2" },
			{ "numpad3", "NumPad 3" },
			{ "numpad4", "NumPad 4" },
			{ "numpad5", "NumPad 5" },
			{ "numpad6", "NumPad 6" },
			{ "numpad7", "NumPad 7" },
			{ "numpad8", "NumPad 8" },
			{ "numpad9", "NumPad 9" },
			{ "o", "O" },
			{ "oem1", "1" },
			{ "oem102", "0" },
			{ "oem2", "2" },
			{ "oem3", "3" },
			{ "oem4", "4" },
			{ "oem5", "5" },
			{ "oem6", "6" },
			{ "oem7", "7" },
			{ "oem8", "8" },
			{ "oembackslash", "\\" },
			{ "oemclear", "OemClear" },
			{ "oemclosebrackets", "]" },
			{ "oemcomma", "," },
			{ "oemminus", "-" },
			{ "oemopenbrackets", "[" },
			{ "oemperiod", "." },
			{ "oempipe", "|" },
			{ "oemplus", "+" },
			{ "oemquestion", "?" },
			{ "oemquotes", "'" },
			{ "oemsemicolon", "OemSemicolon" },
			{ "oemtilde", "`" },
			{ "p", "P" },
			{ "pa1", "Pa1" },
			{ "packet", "Packet" },
			{ "pagedown", "Page Down" },
			{ "pageup", "Page Up" },
			{ "pause", "Pause" },
			{ "play", "Play" },
			{ "print", "Print" },
			{ "printscreen", "PrintScreen" },
			{ "prior", "Prior" },
			{ "processkey", "ProcessKey" },
			{ "q", "Q" },
			{ "r", "R" },
			{ "rbutton", "Right Mouse" },
			{ "rcontrolkey", "R Control" },
			{ "return", "Return" },
			{ "right", "Right" },
			{ "rmenu", "R Alt" },
			{ "rshiftkey", "R Shift" },
			{ "rwin", "RWin" },
			{ "s", "S" },
			{ "scroll", "Scroll" },
			{ "select", "Select" },
			{ "selectmedia", "SelectMedia" },
			{ "separator", "Separator" },
			{ "shift", "Shift" },
			{ "shiftkey", "ShiftKey" },
			{ "sleep", "Sleep" },
			{ "snapshot", "Snapshot" },
			{ "space", "Space" },
			{ "subtract", "Subtract" },
			{ "t", "T" },
			{ "tab", "Tab" },
			{ "u", "U" },
			{ "up", "Up" },
			{ "v", "V" },
			{ "volumedown", "VolumeDown" },
			{ "volumemute", "VolumeMute" },
			{ "volumeup", "VolumeUp" },
			{ "w", "W" },
			{ "x", "X" },
			{ "xbutton1", "XButton1" },
			{ "xbutton2", "XButton2" },
			{ "y", "Y" },
			{ "z", "Z" },
			{ "zoom", "Zoom" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyNameWithModifier = "{00} + {01}";

		public static string FormatKeyName(Intersect.Client.Framework.GenericClasses.Keys modifier, Intersect.Client.Framework.GenericClasses.Keys key)
		{
			LocalizedString localizedString = KeyDictionary[Enum.GetName(typeof(Intersect.Client.Framework.GenericClasses.Keys), key).ToLower()];
			if (modifier != Intersect.Client.Framework.GenericClasses.Keys.None)
			{
				LocalizedString localizedString2 = KeyDictionary[Enum.GetName(typeof(Intersect.Client.Framework.GenericClasses.Keys), modifier).ToLower()];
				localizedString = KeyNameWithModifier.ToString(localizedString2, localizedString);
			}
			return localizedString;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct LoginWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotPassword = "Forgot Password?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Login = "Login";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Password = "Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SavePassword = "Save Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Login";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Username = "Username";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Main
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GameName = "Saiyan Online";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct MenuScene
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PressAnyKey = "Appuie sur une touche";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Play = "Jouer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlayHint = "Reprends ton aventure là où Enma t’a laissé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Register = "Créer un compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegisterHint = "Nouveau venu ? Crée ton compte et entre dans la faille.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Options = "Options";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionsHint = "Affichage, son, touches et interface.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Credits = "Crédits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreditsHint = "Ceux qui ont rendu ce monde possible.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quit = "Quitter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitHint = "Fermer le jeu. Ta progression est sauvegardée.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerOnline = "Serveur en ligne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerReserved = "Serveur fermé aux joueurs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerConnecting = "Connexion…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerOffline = "Serveur hors ligne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerFull = "Serveur plein";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerVersion = "Version du jeu différente";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerUnavailable = "Le serveur ne répond pas pour le moment.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegistrationsClosed = "Les inscriptions sont fermées pour le moment.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Discord = "Discord";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString News = "Nouveautés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NewsTag = "Nouveau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyEnter = "Entrée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyEscape = "Échap";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyTab = "Tab";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintNavigate = "Naviguer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintConfirm = "Valider";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintBack = "Retour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintQuit = "Quitter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintNextField = "Champ suivant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintTurn = "Tourner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintPlay = "Jouer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintLogout = "Déconnexion";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintClass = "Classe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Retour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoginKicker = "Bon retour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoginTitle = "Connexion";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Username = "Nom de compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Password = "Mot de passe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RememberMe = "Se souvenir de moi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotPassword = "Mot de passe oublié ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoginButton = "Se connecter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoginBusy = "Connexion…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoAccount = "Pas encore de compte ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreateAccountLink = "Créer un compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterUsername = "Entre ton nom de compte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterPassword = "Entre ton mot de passe.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegisterKicker = "Nouveau joueur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegisterTitle = "Créer un compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Email = "Adresse e-mail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsernamePlaceholder = "3 caractères minimum";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmailPlaceholder = "toi@exemple.fr";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PasswordPlaceholder = "{00} minimum";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConfirmPassword = "Confirmer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConfirmPlaceholder = "Le même";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegisterButton = "Créer mon compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegisterBusy = "Création…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HaveAccount = "Déjà un compte ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoginLink = "Se connecter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PasswordMismatch = "Les deux mots de passe ne correspondent pas.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmailInvalid = "Cette adresse e-mail n’est pas valide.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotKicker = "Ton compte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotTitle = "Mot de passe oublié";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotText = "Entre ton nom de compte ou ton adresse e-mail : un code pour changer ton mot de passe t’y sera envoyé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UsernameOrEmail = "Nom de compte ou e-mail";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotButton = "Envoyer le code";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgotBusy = "Envoi…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AccountCreated = "Compte créé. Bienvenue dans la faille, guerrier !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreditsKicker = "Saiyan Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreditsTitle = "Crédits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitTitle = "Quitter le jeu ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitText = "Ta progression est sauvegardée à chaque action.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitConfirm = "Quitter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stay = "Rester";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Farewell = "À bientôt, guerrier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WelcomeBack = "Bon retour, {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SelectionTitle = "Sélection du personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourWarrior = "Ton guerrier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShort = "Niv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LastPosition = "Dernière position";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnknownPlace = "Inconnue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tier = "Palier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SignatureTechnique = "Technique signature";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Delete = "Supprimer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Logout = "Déconnexion";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LoggedOut = "Tu es déconnecté.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptyKicker = "Emplacement libre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SlotsLabel = "Tes guerriers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SlotCreate = "+ Créer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintCharacters = "Guerriers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptyTitle = "Aucun guerrier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptyText = "Enma attend ton choix. Crée ton personnage pour entrer dans le monde.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreateWarrior = "Créer ton guerrier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteTitle = "Supprimer {00} ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteText = "Ce guerrier et toute sa progression seront effacés. C’est définitif.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Keep = "Garder";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Deleted = "{00} a été supprimé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Destination = "Destination";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TierBorrowedBody = "Corps d’emprunt";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TierFirstAwakening = "Premier éveil";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TierMastery = "Maîtrise";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TierSummit = "Sommet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NewCharacter = "Nouveau personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChooseWarrior = "Choisis ton guerrier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CreationText = "Coiffure, couleur des cheveux, peau : crée le Saiyan qui te ressemble. Ce choix est définitif.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SpriteComing = "Sprite à venir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassComing = "À venir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SpriteComingText = "Cette classe n’a pas encore son personnage dessiné.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClassComingError = "Cette classe arrive bientôt : choisis-en une autre pour l’instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormsLadder = "Échelle des formes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Outfit = "Tenue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Passive = "Passif";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintOutfit = "Tenue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Coiffure = "Coiffure";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cheveux = "Cheveux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Peau = "Peau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintCoiffure = "Coiffure";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintCheveux = "Cheveux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintPeau = "Peau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureCriniere = "Crinière";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffurePointes = "Pointes dressées";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureFlamme = "Flamme";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureMeches = "Mèches folles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureCarre = "Carré";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureMeche = "Mèche en avant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureEbouriffee = "Ébouriffée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureHerisson = "Hérisson";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CoiffureBroussaille = "Broussaille";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CheveuxNoir = "Noir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CheveuxBrun = "Brun";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PeauClaire = "Claire";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PeauDoree = "Dorée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PeauMate = "Mate";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PeauFoncee = "Foncée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterName = "Nom du personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterNamePlaceholder = "3 à 16 lettres ou chiffres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Create = "Créer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterNameInvalid = "Ce nom n’est pas valide : lettres et chiffres, sans espace.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct MainMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Credits = "Credits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Exit = "Exit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Login = "Login";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Register = "Register";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Settings = "Settings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SettingsTooltip = "";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Start = "Start";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Main Menu";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Settings
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Apply = "Apply";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AudioSettingsTab = "Audio";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoCloseWindows = "Auto-close Windows";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoToggleChatLog = "Auto-toggle chat log visibility";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Cancel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnableLighting = "Enable Light Effects";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowBlood = "Show blood";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CameraEffects = "Camera effects in combat (shake, zoom)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormatResolution = "{00}x{01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormatZoom = "{00}x";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fps120 = "120";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fps30 = "30";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fps60 = "60";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fps90 = "90";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowFPSCounter = "Show FPS Counter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowPingCounter = "Show Ping Counter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fullscreen = "Fullscreen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GameSettingsTab = "Game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InformationSettings = "Information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InterfaceSettings = "Interface";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ControlsTab = "Controls";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Language = "Language";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MusicVolume = "Music Volume: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionVolume = "Volume";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VolumeMusic = "Music";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VolumeSoundEffects = "Sound Effects";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Resolution = "Resolution";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResolutionCustom = "Custom Resolution";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Restore = "Restore Defaults";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SoundVolume = "Sound Volume: {00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowExperienceAsPercentage = "Show experience as percentage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowFriendOverheadHpBar = "Show friends overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowFriendOverheadInformation = "Show friends overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowGuildOverheadHpBar = "Show guild member overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowGuildOverheadInformation = "Show guild member overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowHealthAsPercentage = "Show health as percentage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowNpcOverheadHpBar = "Show NPC overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowNpcOverheadInformation = "Show NPC overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowManaAsPercentage = "Show mana as percentage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowMyOverheadHpBar = "Show my overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowMyOverheadInformation = "Show my overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowPartyOverheadHpBar = "Show party member overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowPartyOverheadInformation = "Show party member overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowPlayerOverheadHpBar = "Show players overhead HP bar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowPlayerOverheadInformation = "Show players overhead information";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SimplifiedEscapeMenu = "Simplified escape menu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StickyTarget = "Sticky Target";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FPS = "FPS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TargetingSettings = "Targeting";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoTurnToTarget = "Auto-turn to target";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSoftRetargetOnSelfCast = "Auto Soft-Retarget on Self-Cast";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSoftRetargetOnSelfCastTooltip = "When this is enabled and an enemy is targeted and a single-target friendly spell is used, the spell will be self-cast without removing the target on the enemy.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Settings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypewriterText = "Typewriter Text";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UIScale = "UI Scale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnlimitedFps = "No Limit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideoSettingsTab = "Video";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vsync = "V-Sync";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnableScrollingWorldZoom = "Enable scrolling to change World Scale in game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WorldScale = "World Scale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WorldScaleTooltip = "World Scale is only available after connecting to the server.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct OptionsWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoCloseWindowsHelp = "Opening a window closes the others";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AutoSoftRetargetHelp = "A support technique goes on you without dropping the enemy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AvailableOnceConnected = "Available once connected";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CameraEffects = "Camera effects in combat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CameraEffectsHelp = "Shake and zoom on heavy hits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CompatibleRendering = "Compatible display";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CompatibleRenderingHelp = "If the ground breaks into stretched stripes: draws it tile by tile (slower)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerformanceMode = "Performance mode";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerformanceModeHelp = "For modest PCs: lighter world drawing, pixels a little less smooth";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerformanceTip = "Your game runs at about {00} frames per second. On a modest PC, try the Performance mode (Graphics, Screen section).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Categories = "SETTINGS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryGame = "Game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryGraphics = "Graphics";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryInterface = "Interface";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryKeys = "Keys";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryOverhead = "Names and bars";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategorySound = "Sound";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionGame = "Targeting and dialogues.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionGraphics = "The picture and its smoothness. Settings marked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionGraphicsEnd = "apply when you confirm.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionInterface = "Windows, gauges and counters.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionKeys = "Click a box, then press the new key. Two keys per action.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionOverhead = "What shows above everyone, in the world.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescriptionSound = "Music and sound effects volume.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DisabledOnServer = "Disabled on this server";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FramesPerSecond = "Frames per second";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyEmpty = "—";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyListening = "Press…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyMoved = "“{00}” was on “{01}”: it has been removed.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeysAction = "ACTION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeysHint = "Right-click a box to clear it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeysPrimary = "PRIMARY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeysSecondary = "SECONDARY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadBar = "HEALTH BAR";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadFriends = "Friends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadGuild = "Guild";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadMe = "Me";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadMeHelp = "your character";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadName = "NAME";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadNpcs = "NPCs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadNpcsHelp = "townsfolk and monsters";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadParty = "Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadPlayers = "Players";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OverheadPlayersHelp = "everyone else";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Preview = "PREVIEW";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewBar = "bar visible, name hidden";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewBoth = "name and health bar visible";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewHovered = "Hovered row: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewName = "name visible, bar hidden";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewNothing = "nothing above the head";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoreKeys = "Default keys";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScrollZoom = "Zoom with the mouse wheel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScrollZoomHelp = "Changes the world scale in game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCamera = "CAMERA";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCombat = "COMBAT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCounters = "COUNTERS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionDialogues = "DIALOGUES";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionEffects = "EFFECTS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionGauges = "GAUGES";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionHotkeys = "HOTKEYS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionMovement = "MOVEMENT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionOther = "OTHER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionScreen = "SCREEN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionSystem = "SYSTEM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionTargeting = "TARGETING";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionLanguage = "LANGUE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Language = "Langue du jeu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LanguageHelp = "S'applique au prochain lancement du jeu.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LanguageRestart = "La nouvelle langue s'appliquera au prochain lancement du jeu.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionWindows = "WINDOWS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StickyTargetHelp = "Clicking elsewhere keeps your target";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VolumeValue = "{00} %";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct PauseMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChangeCharacter = "Change character";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Logout = "Log out";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quit = "Quit the game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Resume = "Resume";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResumeHint = "to resume";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Parties
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InFight = "You are currently fighting!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Members = "Members";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourParty = "Your party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourShare = "Your share of XP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithoutBonus = "instead of {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShareRange = "Sharing range";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tiles = "{00} tiles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RangeHint = "beyond that, no experience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OutOfRange = "out of range";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeSlot = "Free slot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeSlotHint = "invite below, or right-click a warrior in the world";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteHint = "Warrior name...";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InviteButton = "Invite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Alone = "You are not in a party.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AloneHint = "Invite a warrior to share your hunts: the experience of a monster rises with every member.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShort = "Level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InvitePrompt = "{00} has invited you to their party. Do you accept?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kick = "Kick {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickLabel = "Kick";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Leader = "Leader";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaderTip = "Party Leader";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Leave = "Leave Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveTip = "Leave Tip";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Name = "{00} - Lv. {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyInvite = "Party Invite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital0 = "HP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital0Value = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital1 = "MP:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vital1Value = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString You = "You";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Member = "Member";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickButton = "Kick from the party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FarSteps = "far · {00} steps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockedOut = "K.O.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct QuestLog
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Abandon = "Abandon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonPrompt = "Are you sure that you want to quit the quest \"{00}\"?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonTitle = "Abandon Quest: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipCompleted = "COMPLETED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipInProgress = "IN PROGRESS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipNotStarted = "AVAILABLE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "Your quest log is empty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Experience = "Experience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceValue = "+{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemQuantity = "x {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Objective = "CURRENT OBJECTIVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickOne = "Pick a quest from the list.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Progress = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rewards = "REWARDS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardExperience = "{00} EXP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardItem = "{00} x{01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardZeni = "{00} Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepReward = "Step reward: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Zeni = "Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ZeniValue = "+{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCompleted = "COMPLETED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionInProgress = "IN PROGRESS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionNotStarted = "AVAILABLE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Upcoming = "WHAT COMES NEXT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Completed = "Quest Completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CurrentTask = "Current Task:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InProgress = "Quest In Progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotStarted = "Quest Not Started";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaskItem = "{00}/{01} {02}(s) gathered.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TaskNpc = "{00}/{01} {02}(s) slain.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Quest Log";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackPin = "Track on screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackUnpin = "Stop tracking";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackTooltipPin = "Track on screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackTooltipUnpin = "Stop tracking on screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrackFull = "Tracker full: {00} quests at most";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct QuestTracker
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "QUEST TRACKER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Step = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Completed = "Quest complete!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KillNpcs = "Defeat: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GatherItems = "Gather: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EventTask = "Objective in progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rest = " / {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gain = "+{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Follow = "Follow with the arrow";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Following = "Followed by the arrow";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FollowingBack = "Followed by the arrow · click to go back to the story";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Story
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tag = "STORY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Talk = "Talk to {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Find = "Find what comes next";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceDistance = "{00} · {01} tiles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuestDone = "QUEST COMPLETE · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChapterDone = "{00} COMPLETE · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChapterDoneShort = "{00} COMPLETE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Next = "WHAT COMES NEXT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToBeContinued = "To be continued";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ComingSoon = "The next chapters are on their way";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Waiting = "{00} is waiting for you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WaitingAt = "{00} is waiting for you · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuestDoneTag = "Quest complete";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelRequired = "Niveau {00} requis, puis parle à {01}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct QuestOffer
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Accept = "Accept";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Decline = "Decline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rewards = "Rewards:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Quest Offer";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Regex
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Email = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Password = "^[-_=\\+`~!@#\\$%\\^&\\*()\\[\\]{}\\\\|;\\:'\",<\\.>/\\?a-zA-Z0-9]{4,64}$";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Username = "^[a-zA-Z0-9]{2,20}$";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Registration
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConfirmPassword = "Confirm Password:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Email = "Email:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmailInvalid = "The email is invalid.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Password = "Password:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PasswordMismatch = "The passwords do not match.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Register = "Register";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Register";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Username = "Username:";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct PasswordChange
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Back = "Cancel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConfirmNewPassword = "Confirm New Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ErrorNoResetCode = "Please enter your reset code";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ErrorNoOldPassword = "Please enter your previous password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ErrorNoNewPassword = "Please enter your new password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ErrorNoNewPasswordConfirmation = "Please confirm your new password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertTitleError = "Error!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InputLabelResetCode = "Reset code";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InputLabelPassword = "Old Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NewPassword = "New Password";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Submit = "Submit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertTitleSuccess = "Success!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<PasswordResetResultType, LocalizedString> ResponseMessages = new Dictionary<PasswordResetResultType, LocalizedString>
		{
			{
				PasswordResetResultType.InvalidRequest,
				"Unable to change the password because the request was invalid."
			},
			{
				PasswordResetResultType.InvalidToken,
				"Unable to change the password because the reset token or password was invalid or expired."
			},
			{
				PasswordResetResultType.NoUserFound,
				"Unable to change the password because the user could not be found."
			},
			{
				PasswordResetResultType.Success,
				"Successfully changed the password."
			},
			{
				PasswordResetResultType.Unknown,
				"An unknown error occurred while changing the password."
			}
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Password Reset";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Server
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Connecting = "Connecting...";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Failed = "Network Error";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HandshakeFailure = "Handshake Error";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Offline";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ServerFull = "Server is Full";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatusLabel = "Server Status: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unknown = "Unknown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VersionMismatch = "Version Mismatch";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Titles
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Choose = "Pick the title shown under your name.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Achievements and quests unlock titles.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString None = "No title";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Ornaments
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Choose = "Pick the ornament around your name plate.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Levels, achievements and quests unlock ornaments.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString None = "No ornament";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Quantity
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BalanceAfterPurchase = "Balance after purchase";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BalanceAfterSale = "Balance after sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuybackPrice = "Buyback price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Deposit = "Deposit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Drop = "Drop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Earnings = "You will receive";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LimitedBy = "Limited by your {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LimitedByBag = "Limited by the room in your bag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "Max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxLegend = "Max. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offer = "Offer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Retrieve = "Take out";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Revoke = "Take back";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sell = "Sell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Store = "Store";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Total = "Total";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnitPrice = "Unit price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "Withdraw";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Shop
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Buy = "Buy";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "This shop has nothing for sale.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemColumn = "ITEM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PriceColumn = "PRICE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellHint = "Drag an item from your bag here to sell it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Shop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyItem = "Buy Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyItemPrompt = "How many units of {00} would you like to buy?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CannotSell = "This shop does not accept that item!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Costs = "Costs {00} {01}(s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellItem = "Sell Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellItemPrompt = "How many units of {00} would you like to sell?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellPrompt = "Do you wish to sell the item: {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellsFor = "Sells for {00} {01}(s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WontBuy = "Shop Will Not Buy This Item";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct SpellDescription
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bound = "Cannot be unlearned.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CastTime = "Cast Time:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cooldown = "Cooldown:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CooldownGroup = "Cooldown Group:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritChance = "Critical Chance:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CritMultiplier = "Critical Multiplier:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DamageType = "Damage Type:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Distance = "Distance:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DoT = "Damages over Time";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Duration = "Duration:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Effect = "Effect:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Friendly = "Support Spell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HoT = "Heals over Time";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreConsumedResourceBlock = "Ignores Consumed Resources";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreCooldownReduction = "Ignores Cooldown Reduction";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreGlobalCooldown = "Ignores Global Cooldown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreMapBlock = "Ignores Map Blocks";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreResourceBlock = "Ignores Active Resources";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IgnoreZDimension = "Ignores Height Differences";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Instant = "Instant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Multiplier = "{00}x";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Percentage = "{00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegularAndPercentage = "{00} + {01}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScalingPercentage = "Scaling Percentage:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScalingStat = "Scaling Stat:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatBuff = "Stat Buff";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tick = "Ticks every:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tiles = "{00} Tiles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unfriendly = "Damaging Spell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> DamageTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "Physical" },
			{ 1, "Magic" },
			{ 2, "True" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> Effects = new Dictionary<int, LocalizedString>
		{
			{ 0, "" },
			{ 1, "Silence" },
			{ 2, "Stun" },
			{ 3, "Snare" },
			{ 4, "Blind" },
			{ 5, "Stealth" },
			{ 6, "Transforms" },
			{ 7, "Cleanse" },
			{ 8, "Invulnerable" },
			{ 9, "Shield" },
			{ 10, "Sleep" },
			{ 11, "On-Hit" },
			{ 12, "Taunt" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> SpellTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "Combat Spell" },
			{ 1, "Warp to Map" },
			{ 2, "Warp to Target" },
			{ 3, "Dash" },
			{ 4, "Special" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> Stats = new Dictionary<int, LocalizedString>
		{
			{ 0, "Attack" },
			{ 1, "Ability Power" },
			{ 2, "Defense" },
			{ 3, "Magic Resist" },
			{ 4, "Speed" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> StatCounts = new Dictionary<int, LocalizedString>
		{
			{ 0, "Attack:" },
			{ 1, "Ability Power:" },
			{ 2, "Defense:" },
			{ 3, "Magic Resist:" },
			{ 4, "Speed:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> TargetTypes = new Dictionary<int, LocalizedString>
		{
			{ 0, "Self Cast" },
			{ 1, "Targetted - Range: {00} Tiles" },
			{ 2, "AOE" },
			{ 3, "Projectile - Range: {00} Tiles" },
			{ 4, "On Hit" },
			{ 5, "Trap" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> VitalCosts = new Dictionary<int, LocalizedString>
		{
			{ 0, "HP Cost:" },
			{ 1, "MP Cost:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> VitalDamage = new Dictionary<int, LocalizedString>
		{
			{ 0, "HP Damage:" },
			{ 1, "MP Damage:" }
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> VitalRecovery = new Dictionary<int, LocalizedString>
		{
			{ 0, "HP Recovery:" },
			{ 1, "MP Recovery:" }
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Spells
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgetSpell = "Forget Spell";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgetSpellPrompt = "Are you sure you want to forget {00}?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Spells";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Known = "Known";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Count = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarHint = "Drag it onto your hotbar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PickOne = "Pick a technique on the left to read what it does.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Seconds = "{00:0.#} s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bonuses = "Stat bonuses";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Techniques
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Techniques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Close = "Close";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Escape = "Esc";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "{00} · level {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyKiBlast = "Ki blasts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyBeam = "Beams";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyMelee = "Close combat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilySupport = "Support";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyTransformation = "Transformations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyUltimate = "Ultimate";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabKiBlast = "Blasts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabBeam = "Beams";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabMelee = "Melee";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabSupport = "Support";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabTransformation = "Forms";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabUltimate = "Ultimate";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LearnedNone = "none yet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LearnedOne = "1 learned";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LearnedMany = "{00} learned";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptyFamily = "You don't know any technique of this family yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoTechnique = "You haven't learned any technique yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rank = "Rank {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stage = "stage {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StageSeal = "Stage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ready = "Ready to evolve";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Preview = "Preview";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Forget = "Forget this technique";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Mastery = "Mastery";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MasteryComplete = "complete for this stage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MasteryNone = "no mastery";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MasteryHintHit = "Grows when the technique lands";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MasteryHintCast = "Grows each time you use it";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextRank = "Rank {00} in {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxRank = "Max rank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Damage = "Damage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DamageDetail = "rank {00} · with your power";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DamageChosen = "rank {00} (chosen) · with your power";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerShot = "per shot";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerBall = "per ball";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PerHit = "per hit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TechniqueBase = "Technique base";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatShare = "{00} % of your {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Effect = "Effect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoDescription = "No description yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gains = "Rank by rank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowRank = "Rank";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowDamage = "Damage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowBase = "Base";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowHits = "Hits";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowKi = "Ki cost";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowEntry = "Entry (% ki)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowCast = "Charge (s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowCooldown = "Cooldown (s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowDuration = "Duration (s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowLeak = "Leak (%/s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "Max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EachRank = "Each rank:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoGain = "no gain set";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepDamage = "{00} base";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepKi = "{00} ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepCast = "{00} s charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepCooldown = "{00} s cooldown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepDuration = "{00} s effect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepLeak = "{00} % leak";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleDamage = "Rank 5: {00} damage. {01} at rank 1: {02}. Evolving always pulls ahead.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RuleOther = "{00} starts again from the gold values.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hotbar = "Hotbar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarKey = "key {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarNone = "not placed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarHint = "Click a slot to place the technique there.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Lineage = "Lineage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Start = "Start";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Awakening = "Awakening · lv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineageHelp = "Evolving replaces the technique: new name, new values, same key.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineageNone = "No evolution for now.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineageComplete = "Lineage complete";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionRank = "Rank 5 reached";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionLevel = "Level {00} awakening";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Evolve = "Evolve";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NodeDamage = "{00} damage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NodeLeak = "{00} %/s leak";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NodeDuration = "{00} s effect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NodeCost = "{00} ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Evolution = "Evolution";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Continue = "Click to continue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gathering = "Gathering ki…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Toast = "Rank {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Details = "Details";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Simple = "Simple";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextRankPrefix = "At rank {00}:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextStage = "Next stage: {00} · {01}. It replaces the technique and keeps its key.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextStageShort = "Next stage: {00}. It replaces the technique and keeps its key.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineageCompleteSimple = "Lineage complete: this technique is at its peak.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowAll = "Show all";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Collapse = "Collapse";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowUnchanged = "{00} unchanged row";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RowsUnchanged = "{00} unchanged rows";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarHintKeys = "{00} to {01}: place it here. When it evolves, it keeps its key.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WindowLearnedMany = "apprises";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WindowLearnedOne = "apprise";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDrag = "Glisser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintDrag = "sur ta barre, ou";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintTo = "à";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintPoint = "en la pointant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineEntry = "Entrée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineLeak = "Fuite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineCast = "Charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineCooldown = "Recharge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LineDuration = "Durée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ValueHits = "{00} × {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ValueSeconds = "{00} s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ValueEntry = "{00} % du ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ValueLeak = "{00} % / s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KindRank = "{00} · Rang {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MasteryFull = "Maîtrise complète";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OnBarOne = "Sur ta barre : touche {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OnBarMany = "Sur ta barre : touches {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeysAnd = "{00} et {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotOnBar = "Pas sur ta barre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EachRankLine = "Chaque rang : {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RisesOnHit = "La maîtrise monte quand la technique touche.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RisesOnUse = "La maîtrise monte à chaque utilisation.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpForget = "Clic droit : oublier la technique.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpDrag = "Glisse-la sur ta barre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpEvolve = "Clique sur Évoluer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EvolveShort = "Évoluer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReadyBecomes = "Devient {00} et garde sa touche.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarDragHint = "Glisse-la sur une case, ou touches {00} à {01}. Elle garde sa touche en évoluant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragRelease = "Lâche-la sur une case de la barre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragPlace = "Lâche : elle ira sur la touche {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragAlready = "Elle est déjà sur la touche {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DragReplace = "Lâche : elle remplacera {00} (touche {01}).";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Trading
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Accept = "Accept";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InFight = "You are currently fighting!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OfferItem = "Offer Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OfferItemPrompt = "How many units of {00} would you like to offer?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Pending = "Pending";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RequestPrompt = "{00} has invited you to trade items with them. Do you accept?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RevokeItem = "Revoke Item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RevokeItemPrompt = "How many units of {00} would you like to revoke?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TheirOffer = "Their Offer:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Trading with {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TradeRequest = "Trading Invite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Value = "Value: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourOffer = "Your Offer:";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct RequestCard
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyTitle = "PARTY INVITATION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TradeTitle = "TRADE PROPOSAL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyPrompt = "invites you to join their party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TradePrompt = "offers you a trade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelClass = "Lv. {00}  ·  {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Lv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OutOfSight = "Out of sight";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyBonus = "+{00} % EXP per member";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PartyMax = "{00} members max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TradeItems = "Items and Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TradeDouble = "Double confirmation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Decline = "DECLINE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Join = "JOIN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Trade = "TRADE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDecline = "Esc";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyAccept = "Enter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildTitle = "CLAN INVITATION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildPrompt = "invites you to join their clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildName = "Clan {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildChat = "Clan chat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FriendTitle = "FRIEND REQUEST";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FriendPrompt = "wants to add you as a friend";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FriendMessages = "Private messages";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FriendOnline = "Online status";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Accept = "ACCEPT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockTitle = "ON FRAPPE À TA PORTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockPrompt = "frappe à la porte de ta maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockEnters = "Entre chez toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockShowOut = "Raccompagnable";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockOpen = "OUVRIR";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KnockIgnore = "IGNORER";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ConfirmCard
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitTitle = "QUIT GAME";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitPrompt = "Do you really want to quit the game?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitGoodbye = "See you soon!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitSaved = "Progress saved";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitInCombat = "In combat: you stay in game until it ends";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitCriminal = "Criminal: your character stays {00} s in game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuitSuspect = "Suspect: your character stays {00} s in game";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GameName = "Saiyan Online";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Stay = "STAY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quit = "QUIT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "CANCEL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LogoutTitle = "LOG OUT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LogoutPrompt = "Do you really want to log out?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Logout = "LOG OUT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterSelectTitle = "CHANGE CHARACTER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterSelectPrompt = "Go back to character selection?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterSelect = "CHANGE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonTitle = "ABANDON QUEST";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonPrompt = "Do you really want to abandon this quest?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonDetail = "Your progress on this quest will be lost.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Abandon = "ABANDON";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgetTitle = "FORGET TECHNIQUE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgetPrompt = "Do you really want to forget this technique?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ForgetDetail = "You will have to learn it again to use it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Forget = "FORGET";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropTitle = "DROP ITEM";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropPrompt = "Drop this item on the ground?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DropDetail = "Another player may pick it up.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Drop = "DROP";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellTitle = "SELL TO MERCHANT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellPrompt = "Do you want to sell this item?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Earning = "+{00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sell = "SELL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTitle = "BUY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyPrompt = "Buy this item?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Buy = "BUY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawTitle = "WITHDRAW LISTING";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithdrawPrompt = "Withdraw this listing from the market?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quantity = "x{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Withdraw = "WITHDRAW";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveFriendTitle = "REMOVE FRIEND";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveFriendPrompt = "Remove this player from your friends?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Remove = "REMOVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundTitle = "FOUND A CLAN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundPrompt = "Found this clan?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FoundCost = "Cost: {00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Found = "FOUND";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveGuildTitle = "LEAVE CLAN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveGuildPrompt = "Do you really want to leave your clan?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveGuild = "LEAVE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromoteTitle = "PROMOTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DemoteTitle = "DEMOTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankPrompt = "New rank: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Promote = "PROMOTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Demote = "DEMOTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickTitle = "KICK FROM CLAN";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KickPrompt = "Kick this member from the clan?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kick = "KICK";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteCharacterTitle = "DELETE CHARACTER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteCharacterPrompt = "Permanently delete this character?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteCharacterChip = "Cannot be undone";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DeleteCharacter = "DELETE";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TradeWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Trade with {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hint = "Click an item in your inventory to offer it, click it in your offer to take it back.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourInventory = "YOUR INVENTORY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InventoryCount = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourOffer = "YOUR OFFER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TheirOffer = "THEIR OFFER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Zeni = "ZENI";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Wallet = "{00} Zeni";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WalletOffered = "{00} offered";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TotalValue = "TOTAL VALUE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Amount = "{00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ItemsValue = "including {00} Z of items";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ready = "READY";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Waiting = "WAITING";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Balance = "BALANCE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YouGive = "You give";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YouReceive = "You get";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourFavor = "+{00} Z in your favor";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InTheirFavor = "+{00} Z in their favor";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Even = "Even trade";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "CANCEL";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Validate = "CONFIRM TRADE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Validated = "CONFIRMED";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "MAX";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlusThousand = "+1k";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct EscapeMenu
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CharacterSelect = "Characters";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Close = "Close";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExitToDesktop = "Desktop";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Logout = "Logout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Settings = "Settings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Menu";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Numbers
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Billions = "b";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Comma = ",";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Decimal = ".";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Millions = "m";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Thousands = "k";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Update
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Checking = "Checking for updates, please wait.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Done = "Update complete! Launching game.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Error = "Update Error! Check logs for more information.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilesRemaining = "{00} Files Remaining";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PercentComplete = "{00}%";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemainingSize = "{00} Left";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Restart = "Update complete! Relaunch {00} to play.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Updating = "Downloading updates, please wait.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct GameWindow
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EntityNameAndLevel = "{00} [Lv. {01}]";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Disposition
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Titre = "Interface layout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Aide = "Drag a piece to move it · click it to set it up";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiecesAffichees = "Pieces shown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToutRemettre = "Reset all";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Terminer = "Done";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SaPlace = "Reset";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Masquer = "Hide";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Fiche = "{00} · {01} % · {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VieEtKi = "Health and ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cible = "Target";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Raccourcis = "Hotbar";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Menus = "Menus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tchat = "Chat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MiniCarte = "Minimap";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Suivi = "Quest tracker";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Police = "Police";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Effets = "Effects";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Groupe = "Party";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuandCible = "when you target something";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuandSuivi = "your tracked quests";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuandPolice = "when the police notice you";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuandEffets = "active buffs and debuffs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuandGroupe = "your party members";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Haut = "top";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Milieu = "middle";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bas = "bottom";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gauche = "left";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Centre = "centre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Droite = "right";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Dungeons
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateDetail = "Donjon · Niv. {00} – {01} · 1 à {02} joueurs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateChestReady = "Coffre du jour disponible";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateChestLater = "Coffre du jour dans {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptEnter = "Entrer dans le donjon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CardDetail = "Donjon · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CardDetailNoZone = "Donjon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagLevel = "Niv. {00} – {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagPlayers = "1 à {00} joueurs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagRooms = "{00} salle(s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagBest = "Record {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Route = "Parcours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoomDefault = "Salle {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveKillAll = "Tous les monstres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveKillNpc = "Vaincre {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjectiveNone = "Passage libre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeBoss = "Boss";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MoreRooms = "+{00} salle(s) de plus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Group = "Ton groupe · {00} sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GroupSolo = "Seul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MemberLeader = "Chef du groupe · niv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MemberLevel = "Niv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MemberReady = "Prêt";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MemberFar = "Loin";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeSlot = "Place libre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FreeSlotDetail = "Invite quelqu’un depuis Social";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScaleSolo = "Seul, les monstres ont {00} % de leur vie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScaleGroup = "À {00}, les monstres ont {01} % de leur vie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FarNote = "Ceux qui sont loin de l’entrée restent dehors.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotLeaderNote = "Seul {00}, chef du groupe, peut faire entrer le groupe.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestTitle = "Coffre du jour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestReady = "Disponible";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestLater = "Dans {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestRule = "Une fois par jour par personnage, remis à {00} h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestEmpty = "Rien de réglé dans l’éditeur.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BeansRule = "{00} haricot(s) magique(s) pour tout le groupe.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BeansRuleDetail = "Un K.O. en coûte un et te ramène au début de la salle. Plus de haricot : tout le monde ressort.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Later = "Plus tard";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterGroup = "Entrer avec le groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterSolo = "Entrer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyNormal = "Normal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyHard = "Difficile";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyHeroic = "Héroïque";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Difficulty = "Difficulté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyReminder = "Un coffre par jour pour ce donjon, quelle que soit la difficulté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileLevel = "Niv.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileExperience = "Exp ×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileHealth = "Vie {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileDamage = "Dégâts {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileCleared = "Record {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileDiscover = "Pour découvrir le donjon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileNotYet = "Pas encore réussi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileLocked = "Termine le mode {00} pour le débloquer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TileBadge = "Réussi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagBestDifficulty = "Record {00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagNoBest = "Pas de record";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScaleSoloDifficulty = "Seul en {00}, les monstres ont {01} % de leur vie, frappent à {02} % et rapportent ×{03} d’expérience.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ScaleGroupDifficulty = "À {00} en {01}, les monstres ont {02} % de leur vie, frappent à {03} % et rapportent ×{04} d’expérience.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CarryNote = "En chef du groupe, tu emmènes aussi ceux qui n’ont pas débloqué le mode {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestTitleDifficulty = "Coffre du jour · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestRuleRicher = "Une fois par jour par personnage pour ce donjon, remis à {00} h. Plus la difficulté est haute, plus il est riche.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChestRuleDungeon = "Une fois par jour par personnage pour ce donjon, remis à {00} h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HeroicTitle = "Premier Héroïque réussi : titre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnterIn = "En {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityCharge = "charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityRocks = "rochers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityShockwave = "onde de choc";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityEyeBeam = "rayon des yeux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityFissures = "fissures";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityGoldRain = "pluie d’or";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbilityAnd = " et ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ModEnragedLater = "+ {00} une fois enragé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ModFromStart = "{00} dès le début";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ModShorter = "annonces plus courtes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ModLonger = "annonces plus longues";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Achievements = "Succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementsCount = "{00} / {01} · {02} / {03} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementPoints = "{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementCustom = "{00} · propre au donjon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementObtained = "Obtenu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementRuns = "{00} / {01} parties";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementPossible = "Possible dans cette partie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementNotHere = "Pas en {00} : {01} ou plus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AchievementSoloOnly = "Seulement en entrant seul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDifficultyLocked = "Termine d’abord le mode {00} pour choisir cette difficulté.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BannerRoomDifficulty = "{00} · {01} · salle {02} sur {03}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndSubtitle = "Donjon terminé · {00} · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndSolo = "seul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndGroup = "à {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndBestDifficulty = "Record battu ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndNoKo = "aucun K.O.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndExperienceDetail = "× {00} en {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndAchievements = "Succès obtenus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndAchievementsPoints = "+{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndNoAchievement = "Aucun nouveau succès cette fois.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailComplete = "{00} réussi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailNoBeans = "Aucun haricot utilisé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailUnderTarget = "{00}, sous les {01} visées";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailSolo = "Seul en {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailRegular = "{00} victoires";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuiteMissed = "raté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissHits = "Touché {00} fois par {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByCharge = "la charge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByRocks = "les rochers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByShockwave = "l’onde de choc";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByEyeBeam = "le rayon des yeux";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByFissures = "les fissures";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissByGoldRain = "la pluie d’or";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissRoomSeconds = "Porte ouverte en {00}, il fallait {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissRoomKo = "{00} K.O. dans la salle";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissUnderTarget = "{00}, il fallait moins de {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissNoBeans = "{00} haricot(s) utilisé(s)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MissGeneric = "Pas réussi cette fois";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BannerRoom = "{00} · salle {01} sur {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Time = "Temps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Beans = "Haricots du groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Objective = "Objectif";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalKillAll = "Vaincs tous les monstres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalKillNpc = "Vaincs {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalNone = "Avance jusqu’à la porte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalRemaining = "Encore debout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalOpen = "La porte est ouverte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalNext = "Ensuite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GoalLast = "La sortie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveHint = "Abandonner le donjon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BossEnraged = "Phase 2 · Enragé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BossEnrageAt = "Sous {00} % de sa vie, il enrage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertCharge = "Il va charger !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertChargeDetail = "Sors de la bande rouge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertRocks = "Les rochers tombent !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertRocksDetail = "Sors des cercles rouges";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertShockwave = "Onde de choc !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertShockwaveDetail = "Éloigne-toi de lui";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertEyeBeam = "Rayon des yeux !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertEyeBeamDetail = "Sors de sa ligne de mire";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertSweep = "Le rayon balaie !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertSweepDetail = "Passe derrière lui";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertFissures = "Fissures !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertFissuresDetail = "Place-toi entre deux branches";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertGoldRain = "Pluie d’or !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlertGoldRainDetail = "Sors des ombres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveTitle = "ABANDONNER LE DONJON";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeavePrompt = "Tu ressors devant l’entrée, sans le coffre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveDetail = "Ton groupe continue sans toi.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveConfirm = "ABANDONNER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveCancel = "RESTER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndTitle = "Donjon terminé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndTime = "Temps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndBest = "Record : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndNewRecord = "Nouveau record !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndBeans = "Haricots";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndBeansDetail = "{00} K.O. en route";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndKills = "Monstres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndKillsDetail = "Vaincus par le groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndPassage = "Passages";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndFirst = "Premier passage !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndAgain = "Déjà terminé avant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndChestOpened = "Ouvert. Le prochain sera prêt dans {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndChestClosed = "Déjà ouvert aujourd’hui : le prochain sera prêt dans {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndNothing = "Pas de chance : rien d’autre cette fois.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndZeni = "Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndExperience = "Expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndTitleUnlocked = "Titre débloqué";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndFooter = "Chacun ouvre son coffre. La sortie se ferme dans {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndExit = "Sortir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EndStay = "Rester un peu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Duration = "{00} h {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationMinutes = "{00} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultEntered = "Tu entres dans {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultNotLeader = "Seul {00}, chef du groupe, peut faire entrer le groupe.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultTooFar = "Approche-toi de l’entrée du donjon.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultAlreadyInRun = "Tu es déjà dans un donjon.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInvalid = "Ce donjon n’est pas prêt : ses salles manquent dans l’éditeur.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDoorLocked = "La porte est fermée : il reste {00} monstre(s).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDoorLockedNpc = "La porte est fermée : vaincs {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultRoomCleared = "{00} : la porte s’ouvre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultBeanUsed = "{00} est K.O. : un haricot le remet debout ({01} restant).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultFailed = "Plus de haricot : le groupe ressort de {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultLeft = "{00} a quitté le donjon.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultRejoined = "Tu reprends {00} là où tu l’avais laissé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultLeftBehind = "Ton groupe est entré dans {00} sans toi : tu étais trop loin de l’entrée.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Echanges
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Resiste = "RÉSISTE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Choc = "CHOC !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ACoupSur = "À COUP SÛR !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Moitie = "MOITIÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Immunise = "IMMUNISÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Surpris = "SURPRIS !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Recuperation = "RÉCUPÉRATION";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Renvoye = "RENVOYÉ !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quille = "QUILLE !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Kiai = "KIAI !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Renverse = "RENVERSÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Riposte = "RIPOSTE !";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Explosion = "EXPLOSION DE KI";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Teachings
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> MasterNames = new Dictionary<string, LocalizedString>
		{
			["piccolo"] = "Piccolo",
			["tortue"] = "Tortue Géniale",
			["tenshinhan"] = "Tenshinhan",
			["krillin"] = "Krillin"
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> MasterRoles = new Dictionary<string, LocalizedString>
		{
			["piccolo"] = "Encaisseur",
			["tortue"] = "Frappeur",
			["tenshinhan"] = "Tireur de ki",
			["krillin"] = "Soutien"
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> MasterMissions = new Dictionary<string, LocalizedString>
		{
			["piccolo"] = "Protège le groupe",
			["tortue"] = "Abat la cible",
			["tenshinhan"] = "Frappe de loin et en zone",
			["krillin"] = "Fait gagner l'équipe"
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> NodeNames = new Dictionary<string, LocalizedString>
		{
			["provocation"] = "Provocation",
			["peau"] = "Peau de Namek",
			["garde"] = "Garde de fer",
			["interposition"] = "Interposition",
			["riposte"] = "Riposte",
			["ancrage"] = "Ancrage",
			["regeneration"] = "Régénération",
			["cape"] = "Cape lestée",
			["rempart"] = "Rempart",
			["volonte"] = "Volonté du guerrier",
			["mur-namek"] = "Mur de Namek",
			["poings"] = "Poings d'acier",
			["enchainement"] = "Enchaînement",
			["plexus"] = "Frappe au plexus",
			["assaut"] = "Ruée d'assaut",
			["grace"] = "Coup de grâce",
			["oeil"] = "Œil du combattant",
			["brise"] = "Garde brisée",
			["duel"] = "Duel",
			["second"] = "Second souffle",
			["rythme"] = "Maître du combo",
			["style-tortue"] = "Style de la Tortue",
			["affute"] = "Ki affûté",
			["concentration"] = "Concentration",
			["economie"] = "Économie du ki",
			["portee"] = "Longue portée",
			["percant"] = "Ki perçant",
			["eclat"] = "Éclat",
			["rafale"] = "Rafale",
			["distance"] = "Distance de sécurité",
			["calme"] = "Esprit calme",
			["fuite"] = "Fuite maîtrisée",
			["tri-faisceau"] = "Tri-Faisceau",
			["partage"] = "Partage du ki",
			["pas"] = "Pas léger",
			["repos"] = "Repos du dojo",
			["deviation"] = "Déviation",
			["encouragement"] = "Encouragement",
			["reflexes"] = "Réflexes du dojo",
			["reserve"] = "Haricots de réserve",
			["relais"] = "Relais de ki",
			["taiyoken"] = "Taiyoken",
			["veille"] = "Veille",
			["haricot-magique"] = "Haricot magique"
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> NodeEffects = new Dictionary<string, LocalizedString>
		{
			["provocation"] = "Les monstres que tu frappes s'acharnent sur toi pendant {00} s",
			["peau"] = "+{00} de Résistance",
			["garde"] = "La garde bloque {00} de dégâts en plus",
			["interposition"] = "En garde, un allié collé à toi subit {00} de dégâts en moins",
			["riposte"] = "Après une garde réussie, le coup suivant fait +{00} de dégâts",
			["ancrage"] = "Les reculs subis sont réduits de {00}",
			["regeneration"] = "Sous 40 % de PV, rend {00} des PV max par seconde",
			["cape"] = "–{00} de dégâts subis des techniques de ki",
			["rempart"] = "Les alliés à 3 pas de toi subissent {00} de dégâts en moins",
			["volonte"] = "Un coup fatal te laisse à 1 PV, une fois toutes les {00} s",
			["mur-namek"] = "Lever ta garde en combat près d'un allié l'invoque : pendant 5 s, tu prends pour toi la moitié des dégâts subis par les alliés à 3 pas, avec +30 % de Résistance. Recharge 60 s.",
			["poings"] = "+{00} de dégâts des coups",
			["enchainement"] = "Le coup final d'un combo frappe {00} plus fort",
			["plexus"] = "Le coup final a {00} de chances d'étourdir 0,6 s",
			["assaut"] = "Le premier coup après une ruée fait +{00} de dégâts",
			["grace"] = "Le coup final repousse {00} plus loin",
			["oeil"] = "+{00} de critique sur les coups",
			["brise"] = "Le coup final retire {00} de Résistance à la cible pendant 4 s",
			["duel"] = "Contre un ennemi seul à 3 pas à la ronde, +{00} de dégâts",
			["second"] = "Chaque coup final rend {00} du ki max",
			["rythme"] = "+{00} ms pour enchaîner le coup suivant",
			["style-tortue"] = "Après un coup final, pendant 5 s, chaque coup lâche une onde de choc : 40 % de ses dégâts aux ennemis au contact. Recharge 20 s.",
			["affute"] = "+{00} de dégâts des techniques de ki",
			["concentration"] = "La charge de ki se remplit {00} plus vite",
			["economie"] = "Les techniques de ki coûtent {00} de ki en moins",
			["portee"] = "+{00} pas de portée aux techniques de ki",
			["percant"] = "Les techniques de ki ignorent {00} de la Résistance",
			["eclat"] = "Une boule d'énergie inflige {00} de ses dégâts aux ennemis collés à la cible",
			["rafale"] = "Une boule d'énergie a {00} de chances de partir en double",
			["distance"] = "À plus de 4 pas de ta cible, +{00} de dégâts de ki",
			["calme"] = "Au-dessus de 80 % de ki, +{00} de critique des techniques",
			["fuite"] = "Les transformations consomment {00} de ki en moins par seconde",
			["tri-faisceau"] = "Ta prochaine technique de ki frappe trois fois plus fort et te coûte 15 % de tes PV. Recharge 60 s.",
			["partage"] = "Quand tu charges ton ki, l'allié à 3 pas qui en a le moins reçoit {00} de ton ki max. Recharge 12 s",
			["pas"] = "Toi et les alliés à 3 pas : +{00} de Vitesse",
			["repos"] = "Hors combat, toi et les alliés à 3 pas récupérez vos PV {00} plus vite",
			["deviation"] = "{00} de chances de dévier un coup porté à un allié collé à toi",
			["encouragement"] = "Toi et les alliés à 3 pas faites +{00} de dégâts",
			["reflexes"] = "+{00} d'esquive",
			["reserve"] = "Toi ou un allié à 3 pas qui passe sous 20 % de PV en récupère {00}. Une fois toutes les 30 s",
			["relais"] = "Quand tu charges ton ki, les alliés à 3 pas en regagnent à {00} de ta vitesse",
			["taiyoken"] = "Tes coups éblouissent les monstres : ils ralentissent de {00} pendant 2 s",
			["veille"] = "Les effets négatifs sur toi et les alliés à 3 pas durent {00} moins longtemps",
			["haricot-magique"] = "Un allié à 3 pas qui devrait tomber se relève avec 30 % de ses PV. Recharge 3 min."
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TeachingsScreen
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Enseignements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Subtitle = "{00} · niveau {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugeToPlace = "À placer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugePlaced = "Placés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Validated = "Enseignements validés.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RefusedByServer = "Refusé par le serveur : la raison est dans le chat.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChosenMain = "{00} sera ton maître principal une fois validé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChosenSecondary = "{00} sera ton maître secondaire une fois validé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChangesCancelled = "Changements annulés.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipMain = "Maître principal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipSecondary = "Secondaire · {00} max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipLevel = "Niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipSecondaryLevel = "Secondaire au niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipTrialPassed = "Épreuve réussie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipTrialToTry = "Épreuve à tenter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipClosed = "Fermée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChooseMain = "Choisir ce maître";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChooseSecondary = "Prendre en secondaire";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrialName = "« {00} »";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SwearOath = "Prête serment à {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TrialAt = "Épreuve chez {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TierPoints = "PTS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyMastersLevel = "Les maîtres ne se présentent qu'au niveau {00}, après le premier palier.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyPassTrial = "Réussis l'épreuve d'un maître puis prête-lui serment : il t'ouvre sa voie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhySecondaryLevel = "Le maître secondaire se choisit au niveau {00}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhySecondaryTrial = "Réussis l'épreuve de niveau {00} de {01} puis prête-lui serment.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyPassMasterTrial = "Réussis « {00} » chez {01} puis prête-lui serment : il t'ouvre sa voie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AttackNotPath = "« {00} » t'a appris son attaque, pas sa voie : réussis « {01} » chez {02} puis prête-lui serment.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SwearNext = "Retourne voir {00} et prête-lui serment : il t'ouvre sa voie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SwearNextSecondary = "Retourne voir {00} et prête-lui serment : il devient ton maître secondaire.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyPathClosed = "Voie fermée : tes deux maîtres sont choisis.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyMaxRank = "Rang maximum atteint.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyMainOnly = "Réservé au maître principal : le secondaire s'arrête au palier 2.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhySecondaryCap = "Le maître secondaire est limité à {00} points.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyTierLocked = "Le palier {00} s'ouvre à {01} points placés chez {02} : tu en as {03}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyUltimateLocked = "L'ultime s'ouvre à {00} points placés chez {01} : tu en as {02}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyUltimateCost = "L'ultime coûte {00} points : il t'en reste {01}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyNoPoints = "Plus aucun point à placer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyNothingToRemove = "Rien à retirer ici.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyValidatedRank = "Un rang validé ne se retire que chez un maître.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WhyUpperTiers = "Retire d'abord les enseignements des paliers au-dessus.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetHeading = "Enseignement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rank = "Rang {00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EffectLabel = "Effet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CurrentLabel = "Actuel";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotLearned = "Pas encore appris";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextRankLabel = "Rang suivant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FirstRankLabel = "Au rang 1";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MaxRankReached = "Rang maximum atteint";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionUltimate = "Ultime de {00} : coûte {01} points.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionSecondary = "Maître secondaire : {00} / {01} points.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionOpen = "Palier ouvert.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ConditionClosed = "Fermé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetOpen = "OUVERT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetClosed = "FERMÉ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GroupEffectPending = "Effet de groupe : il agira en jeu à l'étape suivante. Ses rangs se placent déjà.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RemoveRank = "Retirer un rang";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Learn = "Apprendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ClickHint = "Clic : apprendre · clic droit : retirer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString YourRole = "TON RÔLE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleNone = "Sans maître";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleNoneBeforeLevel = "Tes points s'accumulent. Au niveau {00}, les maîtres te mettent à l'épreuve : celui à qui tu prêtes serment te donne ton rôle.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleNoneAfterLevel = "Réussis l'épreuve d'un maître puis prête-lui serment : il te donne ton rôle.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleMain = "Disciple de {00} ({01} / {02}). En groupe : {03}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleMainSecondaryLater = "Disciple de {00} ({01} / {02}). En groupe : {03}. Maître secondaire au niveau {04}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleWithTouch = "{00} · touche de {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RoleMainAndSecondary = "Disciple de {00} ({01} / {02}), élève de {03} ({04} / {05}). En groupe : {06}.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddedPower = "Puissance ajoutée (estimée)";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerCap = "Plafond +15 %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuperSaiyanMark = "Super Saiyan +30 %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cancel = "Annuler";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Validate = "Valider";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Sending = "Envoi…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AfterValidation = "Après validation, un rang ou un maître ne se change plus que chez un maître.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct TrialHud
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Eyebrow = "ÉPREUVE · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Hourglass = "SABLIER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusPrefix = "Bonus · ";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Success = "ÉPREUVE RÉUSSIE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Failure = "ÉPREUVE RATÉE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusSuccess = "Bonus réussi : un titre t'attend dans l'onglet Personnage.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Leave = "Abandonner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeavePrompt = "Abandonner l'épreuve ?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeaveDetail = "Tu reviens devant le maître, sans rien perdre. Tu pourras la retenter quand tu veux.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Continue = "Continuer";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Figurants
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> Phrases = new Dictionary<string, LocalizedString>
		{
			["1"] = "Il paraît que le champion s'entraîne au dojo, la nuit…",
			["2"] = "Tu as vu le prix du lait au Z-Mart ?",
			["3"] = "Ma fille veut apprendre le Kamehameha. À six ans !",
			["4"] = "On a encore senti la terre trembler vers les Mesas.",
			["5"] = "Le maire dit que tout va bien. Comme d'habitude.",
			["6"] = "Un type aux cheveux d'or a survolé la tour hier.",
			["7"] = "La banque ferme tôt ce soir.",
			["8"] = "Si tu vas au désert, prends de l'eau. Beaucoup.",
			["9"] = "Les crabes de la plage ont doublé de taille, non ?",
			["10"] = "Mon grand-père dit qu'il a vu un dragon, une fois.",
			["11"] = "Encore une aérovoiture garée sur le trottoir…",
			["12"] = "Le café du coin a changé de patron, c'est moins bon."
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Flight
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NeedTarget = "Choisis une cible pour plonger dessus.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Locked = "Vol impossible · {00} s";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Hdv
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MesAnnonces = "My listings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEncaisser = "To collect";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rien = "nothing";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Tri = "Sort";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UneAnnonce = "1 listing";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NAnnonces = "{00} listings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrdrePrixCroissant = "cheapest first";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrdrePrixDecroissant = "most expensive first";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrdreRecentes = "newest first";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrdreNom = "by name";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrdreJet = "best rolls first";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonnePrix = "Price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideTitre = "Nobody is selling anything yet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideTexte = "Be the first to put an item up for sale: here, players set the prices.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideBouton = "Sell an item";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommentTitre = "How it works";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape1Titre = "List from your bag:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape1 = "you choose the quantity and the price.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape2Titre = "Your listing lasts {00} days.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape2 = "The {00} % tax is only taken if the item sells; unsold, it comes back to your bag.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape3Titre = "Your sales happen even offline:";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etape3 = "your earnings wait for you in My sales.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChoisisAnnonce = "Pick a listing to see the item in detail.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChoisisVente = "Pick one of your listings to see it here.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Bonus = "Bonus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NiveauRequis = "Level required";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Encore = "{00} left";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IlTeRestera = "you will have left";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString IlTeManque = "you are missing";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AToi = "your listing";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TonSac = "Your bag";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TonSacComplement = "pick what you put up for sale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Grises = "Items you wear are greyed out: unequip them to sell them.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PrixCourant = "Current price";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Max = "Max";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Lot = "lot of {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NiveauCourt = "level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unite = "each";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ValeurConseillee = "Suggested value";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Heritage
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SectionCombat = "COMBAT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectionTenue = "Une direction tenue compte aussi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DirectionTenueAide = "Sinon, il faut la taper juste avant le coup";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Housing
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateForSale = "Terrain à vendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateHouseForSale = "Maison à vendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateSizePrice = "{00} × {01} cases · {02} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateOwner = "Maison de {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AccessOpen = "Ouverte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AccessFriends = "Amis et clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AccessLocked = "Verrouillée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptKey = "E";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptViewTerrain = "Voir le terrain";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptViewHouse = "Voir la maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptEnterOwn = "Entrer chez toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptEnter = "Entrer chez {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptLocked = "Verrouillée par {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptFriendsOnly = "Réservée aux amis de {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptKnock = "Frapper à la porte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptKnocked = "Tu as frappé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptKnockedDetail = "{00} est prévenu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptAway = "{00} n’est pas là";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptAwayDetail = "Reviens plus tard";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptNobody = "Personne n’ouvre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptNobodyDetail = "{00} ne t’a pas ouvert";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptTooFar = "Approche-toi de la porte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptBed = "Dormir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptChest = "Ouvrir le coffre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PromptClock = "Voir l’heure";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToastBought = "Maison déployée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToastBoughtText = "Tout le monde voit ta maison, même quand tu n’es pas là.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyForSale = "À vendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTerrain = "Terrain";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyHouse = "Maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuySize = "{00} × {01} cases";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTerrainAlone = "Terrain seul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyPrice = "Prix";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Amount = "{00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyCapsuleRoom = "Pièce {00} × {01} · {02} meubles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTooBig = "Trop grande pour ce terrain ({00} × {01})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyTotalDetail = "Terrain {00} + capsule {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyPrebuiltDetail = "{00}, déjà posée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyRemaining = "Il te restera {00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyMissing = "Il te manque {00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyLater = "Plus tard";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyConfirm = "Acheter et déployer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyConfirmPrebuilt = "Acheter la maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyNoCapsule = "Aucune capsule dans l’éditeur.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DoorTitle = "Porte d’entrée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DoorVisible = "Visible par tous";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionOpen = "Ouverte à tous";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionOpenHelp = "N’importe qui entre par la porte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionFriends = "Amis et clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionFriendsHelp = "Tes amis et ton clan entrent. Les autres frappent.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionLocked = "Verrouillée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionLockedHelp = "Personne n’entre. On peut frapper à la porte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagOpen = "Ouverte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagFriends = "Amis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TagLocked = "Verrouillée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PassersBy = "Ce que voient les passants";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourHouse = "Dans ta maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NobodyInside = "Personne chez toi pour l’instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorFriend = "Ami";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorFriendFemale = "Amie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorGuild = "Clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorGuest = "Visiteur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorGuestFemale = "Visiteuse";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorEntered = "{00} · entré il y a {01} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorEnteredFemale = "{00} · entrée il y a {01} min";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VisitorJustNow = "{00} · entre à l’instant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ShowOut = "Raccompagner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Save = "Enregistrer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HudOwner = "Maison-Capsule de {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HudHome = "Chez toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HudDecorating = "Mode décoration";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CounterFurniture = "Meubles posés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CounterRoom = "Pièce";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HudDecorate = "décorer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Furniture = "Mobilier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InYourBag = "Dans ton sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterAll = "Tout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterComfort = "Confort";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterStorage = "Rangement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterDecor = "Déco";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterWall = "Murs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceFloor = "au sol";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceAgainstWall = "contre un mur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceWall = "au mur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceRug = "au sol, sous les meubles";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CardSize = "{00}×{01} · {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TurnsVariants = "{00} sens dessinés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TurnsMirror = "Se retourne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TurnsNot = "Ne tourne pas";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EffectBed = "Réapparition ici · soin au réveil";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EffectChest = "{00} cases de rangement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EffectClock = "L’heure de l’Âge 761";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quantity = "×{00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StoreAll = "Tout ranger";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Finish = "Terminer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyClick = "Clic";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintPlace = "poser · prendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyRotate = "R";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintRotate = "tourner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDelete = "Suppr";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintStore = "ranger";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyEscape = "Échap";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintDrop = "lâcher";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString KeyDecorate = "H";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintQuit = "quitter la décoration";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BubbleClickToPlace = "Clic pour poser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BubbleRotate = "R : tourner";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BubbleHover = "Clic : déplacer · R : tourner · Suppr : ranger";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rotated = "Tourné : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Mirrored = "retourné";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unmirrored = "à l’endroit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoTurn = "Ne tourne pas : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoTurnDefault = "une seule image";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoRoomToTurn = "Pas la place de tourner ici ({00})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictOutside = "Hors de la pièce";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictWallOnly = "Se pose au mur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictFloorOnly = "Se pose au sol";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictAgainstWall = "Se pose contre le mur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictEntrance = "Garde l’entrée libre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VerdictOccupied = "Place occupée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultNotEnoughZeni = "Il te manque {00} Z.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultTerrainTaken = "Ce terrain vient d’être acheté.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultAlreadyOwner = "Tu as déjà une maison ({00}).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultTooFar = "Tu es trop loin.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDoesNotFit = "Cette capsule ne rentre pas sur ce terrain.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInvalidTerrain = "Ce terrain n’est plus à vendre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultDoorOpened = "{00} entre chez toi.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultShownOut = "{00} t’a raccompagné à la porte.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultAccessSaved = "Porte d’entrée enregistrée.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultFurnitureLimit = "Pas plus de {00} meubles dans cette maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultInventoryFull = "Ton sac est plein";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultChestNotEmpty = "Vide le coffre avant de le ranger";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultNotYourHouse = "Ce n’est pas ta maison.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultCannotDecorate = "Cette maison ne se décore pas.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultSlept = "Tu te sens reposé.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultSleptRespawn = "Tu te sens reposé. Tu réapparaîtras ici.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultStoredAll = "Tout est rangé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultStoredAllLeft = "{00} meuble(s) n’ont pas pu être rangés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceHint = "Clique pour placer la maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceLeft = "Gauche";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceCenter = "Centre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceRight = "Droite";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceBack = "Au fond";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlaceFront = "Devant";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellButton = "Revendre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellTitle = "REVENDRE LA MAISON";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellPrompt = "Tu récupères {00} Z, la moitié de ce qu’elle a coûté.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellDetail = "Tes meubles reviennent dans ton sac et le terrain redevient à vendre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellChip = "+{00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellConfirm = "REVENDRE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SellCancel = "GARDER";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultSold = "Maison revendue : {00} Z rendus. Tes meubles sont dans ton sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultSellNeedsSpace = "Fais de la place dans ton sac pour tes meubles : il manque {00} emplacement(s).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultChestNotEmptySell = "Vide ton coffre avant de revendre la maison.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeButton = "Agrandir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeTag = "Agrandir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeCurrent = "Ta capsule";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeNone = "Aucune capsule plus grande ne rentre sur ce terrain.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeRowPrice = "+{00} Z";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeTotalDetail = "Différence avec ton {00} · meubles gardés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UpgradeConfirm = "Agrandir la maison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultUpgraded = "Maison agrandie : {00}. Tes meubles ont suivi.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultUpgradedReturned = "Maison agrandie : {00}. {01} meuble(s) ne tenaient plus et sont revenus dans ton sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultUpgradeNeedsSpace = "Des meubles ne tiendront plus dans la nouvelle pièce : il manque {00} emplacement(s) dans ton sac.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultUpgradeChestBlocked = "Un coffre ne tiendra plus dans la nouvelle pièce : vide-le avant d’agrandir.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultCannotUpgrade = "Cette maison ne peut pas être agrandie.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultFolded = "Ta maison s’est repliée en capsule après {01} jours d’absence. Ton {00} et tes meubles t’attendent : redéploie-la sur un terrain libre, tu ne paies que le terrain.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultFoldedDoesNotFit = "Redéploie d’abord ta capsule repliée ({00}) : il lui faut un terrain à bâtir assez grand.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyFoldedOwned = "À toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyFoldedDetail = "Ta capsule repliée, meubles compris";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyFoldedBlocked = "Ta capsule repliée doit être redéployée d’abord";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BuyConfirmFolded = "Redéployer ma capsule";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Journal
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RechercheIndice = "Search for a quest, a place, a monster…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FiltreToutes = "All";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FiltreSuivies = "Tracked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatEnCours = "In progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatHistoire = "Story";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatEnseignements = "Teachings";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatAnnexes = "Side quests";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatQuotidiennes = "Daily";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatContrats = "Contracts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatAFaire = "To do";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CatTerminees = "Completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RubriqueFini = "Done";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GroupePrimes = "Police bounties";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GroupeCriminels = "Wanted criminals";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GroupeRuban = "Red Ribbon";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailNEnCours = "{00} in progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailQuotidienne = "resets every day";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailContrats = "{00} / {01} in progress · always tracked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailPrimes = "one target per bounty · cash in at a board";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailCriminels = "a race: the first to take them down gets the bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DetailRuban = "one contract at a time · until midnight";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneQuete = "Quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneOu = "Progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColonneRecompense = "Reward";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitreRecherche = "Search";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuetesN = "{00} quests";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString QuetesUne = "1 quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ContratsN = "{00} contracts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ContratsUn = "1 contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousTitreContrats = "{00} / {01} in progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RelireHistoire = "the story to read again";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultatsN = "{00} results for “{01}”";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ResultatUn = "1 result for “{00}”";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideEnCours = "No quest in progress. Follow the story: go and see who needs you.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideContrats = "No contract in progress: police bounties are taken at the wanted board.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideRecherche = "Nothing for “{00}”.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideTerminees = "No quest completed yet.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideSuivies = "Nothing tracked on screen: a quest's pin adds it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VideCategorie = "Nothing here for now.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedSuivies = "Tracked on screen: {00} / {01} quests · contracts always are";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedContrats = "Contracts are taken at the wanted board; Red Ribbon ones show here too";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PiedTerminees = "{00} in the story · {01} outside it · latest first";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnteteHistoireValeur = "{00} · {01} / {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EnteteSuivies = "Tracked on screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CarteQuete = "Quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CarteSuite = "Next: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CarteFin = "The whole story so far is played.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEcran = "On screen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEcranContrats = "+ {00} contracts, always shown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEcranContratUn = "+ 1 contract, always shown";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEcranAide = "A quest's pin puts it in the tracker, on the right of the screen ({00} at most).";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AEcranHistoireRetiree = "Story card hidden";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HistoireRetirer = "Remove the Story card from the screen · the arrow still follows the story";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HistoireRemettre = "Show the Story card again · it doesn't count toward the {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousAnnexe = "Side quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousQuotidienne = "Daily quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousEnseignement = "Teaching";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousPrime = "Police bounty · level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousPrimeRemplie = "Police bounty · done {00} ago";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousCriminel = "Wanted criminal";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SousRuban = "Red Ribbon · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OuTerminee = "Completed";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OuAFaire = "To take";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecompenseExp = "+{00} exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecompenseRep = "+{00} rep.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecompensePrime = "bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OeilAide = "Always in the tracker";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipAnnexe = "Side quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipQuotidienne = "Daily";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ChipEnseignement = "Teaching";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VitrineEtape = "Step";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VitrineEtapes = "Steps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VitrineNiveau = "Level";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FichePrimeSous = "Bounty of the day · always tracked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FicheCriminelSous = "Wanted criminal · always tracked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FicheRubanSous = "Red Ribbon contract · always tracked";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Etapes = "Steps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EtapesEnPlus = "+ {00} more steps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RecompensesRecues = "Rewards received";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LeContrat = "The contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjVaincre = "Defeat {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjNeutraliser = "Take down {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ObjAbattre = "Take down the Hero {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleRepere = "Spotted";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReglePrime = "Bounty";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleJusqua = "Until";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleVu = "Seen";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleEncaisser = "Cash in";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReglePaye = "Pay";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleOu = "Where";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ReglePrimeTexte = "to whoever holds the contract and {00}% of the damage (a party counts together), cashed in at a board";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleMinuit = "midnight · {00} / {01} contracts in progress";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleEncaisserTexte = "at a wanted board";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleCriminelPrime = "{00} Z, to the first who takes them down";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RegleRubanMinuit = "midnight · lost if you die or get arrested";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BoutonAbandonQuete = "Abandon the quest";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BoutonAbandonContrat = "Abandon the contract";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoteTerminee = "A completed quest stays here so you can read the story again.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoteAFaire = "It is taken from whoever offers it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoteRemplie = "Cash it in at a wanted board.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonContratTitre = "ABANDON THE CONTRACT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonContratQuestion = "Do you really want to abandon this contract?";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonContratPrime = "You can take it again at the wanted board.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonContratCriminel = "You can take it again at the board while they are wanted.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AbandonContratRuban = "You won't be able to take it again today.";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Weather
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> Names = new Dictionary<string, LocalizedString>
		{
			["soleil"] = "Grand soleil",
			["nuageux"] = "Nuageux",
			["venteux"] = "Grand vent",
			["ventfroid"] = "Vent glacial",
			["pluie"] = "Pluie",
			["orage"] = "Orage",
			["brumemer"] = "Brume de mer",
			["canicule"] = "Canicule",
			["ventsable"] = "Vent de sable",
			["tempete"] = "Tempête de sable",
			["averse"] = "Averse",
			["eclaircie"] = "Éclaircie",
			["couvert"] = "Couvert",
			["pluiefine"] = "Pluie fine",
			["brouillard"] = "Brouillard",
			["froid"] = "Ciel froid",
			["nuageuxfroid"] = "Nuageux et froid",
			["neige"] = "Neige",
			["blizzard"] = "Tempête de neige",
			["givre"] = "Brouillard givrant",
			["calme"] = "Aucune"
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct WorldBoss
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeOngoing = "BOSS MONDIAL · EN COURS";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeSoon = "BOSS MONDIAL · BIENTÔT";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Difficulty = "DIFFICULTÉ {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyNormal = "Normale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyReinforced = "Renforcée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyHigh = "Élevée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DifficultyMaximum = "Maximale";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Players = "{00} joueurs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HealthPercent = "{00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Shield = "BOUCLIER · brisez les cristaux ({00} / {01})";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Victory = "BOSS MONDIAL VAINCU";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Retreat = "LE COLOSSE SE RETIRE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Summary = "{00} · {01} · {02} · {03} combattants";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Damage = "Dégâts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Healing = "Soins";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AggroHeld = "Aggro tenue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Points = "{00} points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rank = "{00}e / {01} · {02} jetons · {03} tirage(s)";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Personnage
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Thread = "{00} · {01} · Niveau {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabEquipment = "Équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabStats = "Caractéristiques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabCustomization = "Personnalisation";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TabSoon = "Bientôt";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Zeni = "Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Places = "Places";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WornHeading = "Équipement porté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TurnHint = "Tourner le personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetsHeading = "Panoplies";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetsDetail = "bonus de 2, 3 et 4 pièces";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoSet = "Aucune pièce de panoplie portée.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetCount = "{00} / {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetCountChange = "{00} → {01} / {02}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetWithItem = "avec cet objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetMissing = "Il manque";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetInBag = "dans ton sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetNextTier = "encore {00} pour « {01} »";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SetComplete = "complète";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagHeading = "Sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagCount = "{00} objets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BagFiltered = "{00} sur {01} objets";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Search = "Rechercher un objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortNone = "Aucun tri";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortRarity = "Rareté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortName = "Nom";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortType = "Type";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortQuantity = "Quantité";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortValue = "Valeur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryAll = "Tout";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryEquipment = "Équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryConsumables = "Consommables";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryTechniques = "Techniques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryResources = "Ressources";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryQuest = "Quête";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryBags = "Sacs";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlacesUsed = "{00} / {01} places";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarHeading = "Barre de raccourcis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HotbarDetail = "choisis un objet, puis clique une case";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintDoubleClick = "Double-clic : équiper ou utiliser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HintDrag = "Glisser : ranger, équiper, raccourci";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetHeading = "Objet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetWorn = "porté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SheetEmpty = "Choisis un objet du sac ou un emplacement.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeEquipment = "Équipement · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeConsumable = "Consommable";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeResource = "Ressource";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeQuest = "Objet de quête";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeBag = "Sac";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TypeTechnique = "Technique";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Unique = "Unique";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Roll = "jet {00} %";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ceiling = "Plafond {00} · {01} Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Price = "{00} Zénis";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPrice = "Invendable";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ComparedTo = "Comparé à {00}, que tu portes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnStat = "Caractéristique";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnValue = "Valeur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnGap = "Écart";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnEffect = "Effet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresHealth = "Vie rendue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RestoresKi = "Ki rendu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GivesExperience = "Expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Cooldown = "Recharge";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Seconds = "{00} s";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SourcesHeading = "Où l'obtenir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SourceCraft = "Fabrication · {00}, niveau {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SourceDrop = "Butin · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SourceHarvest = "Récolte · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SourceNone = "Aucune source connue pour l'instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionEquip = "Équiper";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionUse = "Utiliser";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionUnequip = "Retirer";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionOpen = "Ouvrir";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ActionDrop = "Jeter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewGaps = "Écarts si tu équipes {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoEquipmentShare = "sans équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerHeading = "Puissance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerNote = "le chiffre des classements, sans les transformations";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PowerChange = "{00} si tu équipes {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ProgressionHeading = "Progression";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelDetail = "niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceLabel = "Expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Health = "Vie";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Ki = "Ki";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusesHeading = "Bonus d'équipement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BonusesDetail = "panoplies comprises";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormsHeading = "Formes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormsDetail = "rang de maîtrise";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormRank = "Rang {00} sur {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FormsMore = "et {00} de plus · touche K";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PreviewHeading = "Aperçu de la plaque";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateHeading = "Ta plaque";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateDetail = "visible au survol en jeu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateTitle = "Titre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PlateOrnament = "Ornement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TryHint = "Survole un titre ou un ornement pour l'essayer.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TryUnlocked = "Essai : {00} · clique pour le porter";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TryLocked = "Essai : {00} · pas encore débloqué";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitlesHeading = "Titres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrnamentsHeading = "Ornements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UnlockedCount = "{00} sur {01} débloqués";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoFolder = "Sans dossier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeWorn = "Porté";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeLocked = "Verrouillé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BadgeLevel = "Niveau {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<int, LocalizedString> EffectDescriptions = new Dictionary<int, LocalizedString>
		{
			{ 1, "Recharge plus vite tes techniques et tes objets." },
			{ 2, "Rend en vie une part des dégâts infligés." },
			{ 3, "Raccourcit étourdissements, entraves et sommeils." },
			{ 4, "Plus de chances de butin sur les monstres." },
			{ 5, "S'ajoute à l'expérience que tu gagnes." },
			{ 7, "Moins de fuite de ki : ta forme tient plus longtemps." },
			{ 8, "Plus de ki rendu à chaque recharge de ki." }
		};
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct PresenceDiscord
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Option = "Show my activity on Discord";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionAide = "Your Discord friends see that you are playing Saiyan Online, your level, your form and the map you are on.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Menu = "In the menu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Niveau = "Level {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NiveauEnCombat = "Level {00} · In combat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BoutonDiscord = "Join the Discord";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BoutonSite = "Game website";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Saiyan = "Saiyan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuperSaiyan = "Super Saiyan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuperSaiyan2 = "Super Saiyan 2";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuperSaiyanGod = "Super Saiyan God";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString UltraInstinct = "Ultra Instinct";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Rankings
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PodiumWeek = "Podium de la semaine";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Classements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Thread = "Progression";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString MenuPlace = "{00}e";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Week = "Cette semaine";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllTime = "Depuis toujours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugePlace = "Ta place";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugeNext = "Prochain calcul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Computed = "Calculé il y a {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WeekReset = "Remise à zéro lundi 6 h, dans {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SinceStart = "Tout ce qui a été fait depuis l’arrivée des classements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotReady = "Premier calcul en cours : reviens dans une minute.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Loading = "Chargement…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "Personne n’est classé ici pour l’instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString EmptyWeek = "Personne n’a encore rien gagné ici cette semaine.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FicheEmpty = "La fiche du premier classé s’affichera ici.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Classified = "{00} classés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyCharacter = "Personnage";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyDungeons = "Donjons";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyAchievements = "Succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyProfessions = "Métiers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyGuilds = "Clans";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilyKarma = "Karma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Level = "Niveau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Power = "Puissance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Achievements = "Points de succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Gathering = "Récolte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Crafting = "Artisanat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Guilds = "Puissance des clans";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildLevel = "Niveau des clans";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescGuildLevel = "Le niveau du clan, puis son expérience pour départager.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescGuildLevelWeek = "L’expérience de clan gagnée par ses membres depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Heroes = "Les plus héroïques";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Feared = "Les plus redoutés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescLevel = "Le niveau du personnage, puis son expérience pour départager.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescLevelWeek = "Les niveaux gagnés depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescPower = "La puissance de la plaque : niveau, caractéristiques et équipement, sans les transformations.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescPowerWeek = "La puissance gagnée depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescDungeon = "Le meilleur temps, de l’entrée à la victoire.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescDungeonWeek = "Le meilleur temps réalisé cette semaine.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescAchievements = "Tous les succès, socle des donjons et carrière compris.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescAchievementsWeek = "Les points de succès gagnés depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescProfession = "L’expérience de métier accumulée ; le niveau est dessous.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescProfessionWeek = "L’expérience de métier gagnée depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescGuilds = "La puissance cumulée des 20 meilleurs membres du clan.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescGuildsWeek = "La puissance gagnée par les 20 meilleurs membres depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescHeroes = "Le karma le plus haut : ceux qui protègent.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescHeroesWeek = "Le karma gagné depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescFeared = "Le karma le plus bas : ceux qu’on évite.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DescFearedWeek = "Le karma perdu depuis lundi 6 h.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Reward = "Récompense de la semaine : « {00} » au 1er, « {01} » aux 2e et 3e, pendant 7 jours.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardDungeon = "Récompense de la semaine en Héroïque : « {00} » au 1er, « {01} » aux 2e et 3e, pendant 7 jours.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllClasses = "Toutes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AllProfessions = "Tous";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Solo = "Seul";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Group = "En groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnRank = "Rang";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnEvolution = "Évol.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnPlayer = "Joueur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnClass = "Classe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnGuild = "Clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnTeam = "Groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnLeader = "Chef";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnMembers = "Membres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnValue = "Valeur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnTime = "Temps";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ColumnGained = "Gagné";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString New = "Nouveau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoGuild = "Sans clan";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelShort = "Niv. {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString LevelsGained = "+{00} niv.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Points = "{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PointsGained = "+{00} pts";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ExperienceDetail = "{00} exp";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ProfessionExp = "exp de métier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ProfessionLevels = "niveaux cumulés : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Karma = "karma";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Members = "{00} membres";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TeamWith = "avec {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Crown = "N°1";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CrownWeek = "N°1 de la semaine";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Me = "Toi";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GapMore = "{00} de plus pour la {01}e place";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GapTime = "{00} de moins pour la {01}e place";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GapFirst = "Personne devant toi.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotRanked = "Tu n’es pas classé ici pour l’instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NotRankedGuild = "Ton clan n’est pas classé : rejoins-en un depuis Social → Clan.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OtherPlaces = "Ses meilleures places";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoPlaces = "Pas encore dans un top.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Online = "En ligne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Offline = "Hors ligne";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AddFriend = "Ajouter en ami";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Invite = "Inviter dans le groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ProfileMeta = "{00} · niveau {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatLevel = "Niveau";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatPower = "Puissance";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StatPoints = "Succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GuildLeader = "Chef : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationDays = "{00} j {01} h";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationHours = "{00} h {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString DurationMinutes = "{00} min";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct MenuWheel
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Title = "Menus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Quests = "Quêtes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Map = "Carte";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rewards = "Récompenses";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Teachings = "Enseignements";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpGroup = "A : ouvrir · B : retour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpOpenGroup = "Relâche pour voir le groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString HelpRoot = "Oriente le stick, relâche pour ouvrir";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Recherche
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Titre = "POLICE";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Vu = "Seen by {00} witnesses";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VuUn = "Seen by one witness";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TemoinParti = "A witness ran to warn the police";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString PersonneVu = "Nobody saw you: no police";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Police = "A police officer is coming";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Enquete = "The police are investigating";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Poursuite = "Chased by the police!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Amende = "Caught: {00} Zeni fine";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AmendeVide = "Caught: a warning";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Prison = "Arrested: off to prison";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Semee = "You lost the police";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Karma = "Karma {00} · {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> Cris = new Dictionary<string, LocalizedString>
		{
			["1"] = "Help!",
			["2"] = "Call the police!",
			["3"] = "Run!",
			["4"] = "He's out of his mind!"
		};

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Chien = "Woof!";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SabotageQui = "A building at {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SabotageLigne1 = "Bring it down: a red cross floats above it.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SabotageLigne2 = "The police will see you there: be quick.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SabotageLigne3 = "{00}% of the pay goes into your bounty.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuiviSabotage = "Sabotage: {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SuiviSabotageOu = "Bring it down at {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString AlerteSaboteur = "A Red Ribbon saboteur is preparing a strike: {00}";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Ressources
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Requis = "{00} · niveau {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Libre = "{00} · sans niveau requis";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Succes
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Thread = "Progression";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugePoints = "Points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString GaugeCount = "Succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Overview = "Vue d'ensemble";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Categories = "Catégories";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Other = "Divers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RailHint = "Un clic sur une carte ouvre sa fiche. La molette fait défiler.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CollectorRank = "Rang de collectionneur";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankBronze = "Bronze";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankSilver = "Argent";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankGold = "Or";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankPlatinum = "Platine";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RankLegend = "Légende";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NextRank = "Encore {00} points pour le rang {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TopRank = "Le plus haut rang";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitlesWon = "Titres gagnés";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString WithinReach = "À ta portée";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Finest = "Tes plus beaux succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingYet = "Rien d'obtenu pour l'instant : le premier est à ta portée.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterAll = "Tous";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterOngoing = "En cours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FilterDone = "Obtenus";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NothingHere = "Rien ici avec ce filtre.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CategoryCount = "{00} / {01} · {02} points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateDone = "Obtenu";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StateOngoing = "En cours";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString ToDo = "À faire";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Done = "Fait";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Complete = "Complet";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Progression = "Progression";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Steps = "Paliers · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString StepsPlain = "Paliers";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Missing = "Il te manque {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Finished = "Terminé";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Rewards = "Récompenses";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleChip = "Titre";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OrnamentChip = "Ornement";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString TitleCaption = "Le titre, tel qu'il sera sous ton nom";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardOrnament = "Ornement : {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardExperience = "{00} points d'expérience";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardCurrency = "{00} {01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString RewardItem = "{00} ×{01}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Worth = "Vaut {00} points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Empty = "Aucun succès pour l'instant.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchTool = "Rechercher un succès";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SearchPlaceholder = "Nom d'un succès, d'un monstre…";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Results = "Résultats · {00}";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NoResult = "Aucun succès ne correspond.";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortTool = "Trier";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortHint = "Changer l'ordre des cartes";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortName = "Nom";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortPoints = "Points";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString SortProgress = "Progression";
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct Tchat
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Monde = "Monde";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandeMonde = "parler à toutes les langues";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString CommandeGlobal = "parler dans ta langue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Traduit = "traduit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Original = "original";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString NonTraduit = "non traduit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VoirOriginal = "clique pour voir l'original";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString VoirTraduction = "clique pour revoir la traduction";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString BoutonPhrases = "Phrases";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString InfobullePhrases = "Des phrases toutes faites : chacun les lit dans sa langue";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilleGroupe = "Groupe";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilleAide = "Aide";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamilleCommerce = "Commerce";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString FamillePolitesse = "Politesse";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString Retour = "Retour";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionTraduire = "Traduire le tchat";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static LocalizedString OptionTraduireAide = "Décochée : un clic sur un message le traduit";

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public static Dictionary<string, LocalizedString> Phrases = new Dictionary<string, LocalizedString>
		{
			["g1"] = "Je cherche un groupe pour : {00}",
			["g2"] = "Il nous manque un joueur",
			["g3"] = "Je suis prêt",
			["g4"] = "Attendez-moi",
			["a1"] = "J'ai besoin d'aide ici",
			["a2"] = "Où se trouve : {00} ?",
			["a3"] = "Suis-moi",
			["c1"] = "Je vends : {00}",
			["c2"] = "Je cherche : {00}",
			["c3"] = "Combien ?",
			["p1"] = "Bonjour à tous",
			["p2"] = "Merci !",
			["p3"] = "Bien joué !",
			["p4"] = "À bientôt"
		};
	}

	private const string StringsFileName = "client_strings.json";

	private static char[] mQuantityTrimChars = new char[2] { '.', '0' };

	private static string[] _unitsBits = new string[5]
	{
		string.Empty,
		"Ki",
		"Mi",
		"Gi",
		"Ti"
	};

	private static string[] _unitsBytes = new string[5]
	{
		string.Empty,
		"K",
		"M",
		"G",
		"T"
	};

	private static readonly MethodInfo _methodInfoDeserializeDictionary = typeof(Strings).GetMethod("DeserializeDictionary", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

	private static readonly MethodInfo _methodInfoSerializeDictionary = typeof(Strings).GetMethod("SerializeDictionary", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

	public static string FormatBits(long quantity)
	{
		double num = ((quantity < 2) ? 0.0 : Math.Log2(quantity));
		double num2 = Math.Max(0.0, Math.Floor(num - 3.3));
		int num3 = (int)Math.Clamp(Math.Floor(num2 / 10.0), 0.0, _unitsBits.Length - 1);
		double num4 = Math.Pow(2.0, num3 * 10);
		double value = (double)quantity / num4;
		string value2 = _unitsBits[num3];
		return $"{value:0.##}{value2}B";
	}

	public static string FormatBytes(long quantity)
	{
		double num = ((quantity < 10) ? 0.0 : Math.Floor(Math.Log10(quantity)));
		double num2 = Math.Max(0.0, num - 1.0);
		int num3 = (int)Math.Clamp(Math.Floor(num2 / 3.0), 0.0, _unitsBytes.Length - 1);
		double num4 = Math.Pow(10.0, num3 * 3);
		double value = (double)quantity / num4;
		string value2 = _unitsBytes[num3];
		return $"{value:0.##}{value2}B";
	}

	public static string FormatQuantityAbbreviated(long value)
	{
		if (value == 0L)
		{
			return "";
		}
		double num = 0.0;
		string text = string.Empty;
		if (value <= 999)
		{
			num = value;
		}
		else if (value >= 1000 && value <= 999999)
		{
			num = (double)value / 1000.0;
			text = Numbers.Thousands;
		}
		else if (value >= 1000000 && value <= 999999999)
		{
			num = (double)value / 1000000.0;
			text = Numbers.Millions;
		}
		else
		{
			if (value < 1000000000 || value > 999999999999L)
			{
				return "OOB";
			}
			num = (double)value / 1000000000.0;
			text = Numbers.Billions;
		}
		if (num >= 10.0)
		{
			return Math.Floor(num) + text;
		}
		return (Math.Floor(num * 10.0) / 10.0).ToString("F1").TrimEnd(mQuantityTrimChars).ToString()
			.Replace(".", Numbers.Decimal) + text;
	}

	private static void SynchronizeConfigurableStrings()
	{
		if (Options.Instance == null)
		{
			return;
		}
		IEnumerable<(int, string)> enumerable = Options.Instance.Items.RarityTiers.Select((string rarityName, int rarity) => (rarity: rarity, rarityName: rarityName));
		foreach (var (value, text) in enumerable)
		{
			if (!ItemDescription.Rarity.ContainsKey(text))
			{
				ItemDescription.Rarity[text] = $"{value}:{text}";
			}
		}
	}

	private static void PostLoad()
	{
		Program.OpenGLLink = Errors.OpenGlLink.ToString();
		Program.OpenALLink = Errors.OpenAllLink.ToString();
	}

	public static void Load()
	{
		Langue.Lire();
		SynchronizeConfigurableStrings();
		try
		{
			Dictionary<string, Dictionary<string, object>> dictionary = new Dictionary<string, Dictionary<string, object>>();
			dictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(Coffre.LireTexte(Path.Combine(ClientConfiguration.ResourcesDirectory, "client_strings.json")));
			Type typeFromHandle = typeof(Strings);
			Type[] nestedTypes = typeFromHandle.GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			foreach (Type item in nestedTypes.OrderBy((Type t) => t.Name, new OrdinalComparer()))
			{
				if (!dictionary.TryGetValue(item.Name, out var value))
				{
					list.Add(item.Name ?? "");
					dictionary[item.Name] = SerializeGroup(item);
					continue;
				}
				FieldInfo[] fields = item.GetFields(BindingFlags.Static | BindingFlags.Public);
				foreach (FieldInfo fieldInfo in fields)
				{
					object value2 = fieldInfo.GetValue(null);
					if (!value.TryGetValue(fieldInfo.Name, out var value3))
					{
						string text = value.Keys.FirstOrDefault((string key) => string.Equals(fieldInfo.Name, key, StringComparison.OrdinalIgnoreCase));
						if (text != null)
						{
							value.TryGetValue(text, out value3);
						}
					}
					if (!(value2 is LocalizedString localizedString))
					{
						if (!(value2 is Dictionary<int, LocalizedString> dictionary2))
						{
							if (value2 is Dictionary<string, LocalizedString> dictionary3)
							{
								DeserializeDictionary(list, item, fieldInfo, value2, value, value3, dictionary3);
								continue;
							}
							Type fieldType = fieldInfo.FieldType;
							if (!fieldType.IsGenericType || typeof(Dictionary<, >) != fieldType.GetGenericTypeDefinition())
							{
								ApplicationContext.Context.Value?.Logger.LogError(new NotSupportedException($"Unsupported localization type for {item.Name}.{fieldInfo.Name}: {fieldInfo.FieldType.FullName}"), "Invalid field type {Type}", fieldType);
								continue;
							}
							Type[] genericTypeArguments = fieldType.GenericTypeArguments;
							Type type = genericTypeArguments.Last();
							if (type != typeof(LocalizedString))
							{
								ApplicationContext.Context.Value?.Logger.LogError(new NotSupportedException($"Unsupported localization dictionary value type for {item.Name}.{fieldInfo.Name}: {type.FullName}"), "Unsupported localization dictionary value type for {GroupName}.{FieldName}: {ParameterTypeName}", item.Name, fieldInfo.Name, type.FullName);
							}
							else
							{
								MethodInfo methodInfo = _methodInfoDeserializeDictionary.MakeGenericMethod(genericTypeArguments.First());
								methodInfo.Invoke(null, new object[7] { list, item, fieldInfo, value2, value, value3, value2 });
							}
						}
						else
						{
							DeserializeDictionary(list, item, fieldInfo, value2, value, value3, dictionary2);
						}
						continue;
					}
					string text2 = (string)value3;
					if (text2 == null)
					{
						ApplicationContext.Context.Value?.Logger.LogWarning(item.Name + "." + fieldInfo.Name + " is null.");
						list.Add(item.Name + "." + fieldInfo.Name + " (string)");
						value[fieldInfo.Name] = (string)localizedString;
						continue;
					}
					LocalizedString localizedString2 = null;
					try
					{
						localizedString2 = fieldInfo.GetValue(null) as LocalizedString;
					}
					catch
					{
					}
					LocalizedString localizedString3 = new LocalizedString(text2);
					if (localizedString2?.ArgumentCount != localizedString3.ArgumentCount)
					{
						list2.Add($"{item.Name}.{fieldInfo.Name} expected {localizedString2?.ArgumentCount.ToString() ?? "ERROR"} argument(s) but the loaded string had {localizedString3.ArgumentCount}");
					}
					fieldInfo.SetValue(null, localizedString3);
				}
			}
			if (list.Count > 0)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Missing strings, overwriting strings file:\n\t{Strings}", string.Join(",\n\t", list));
				SaveSerialized(dictionary);
			}
			if (list2.Count > 0)
			{
				ApplicationContext.Context.Value?.Logger.LogWarning("Argument count mismatch on {MismatchCount} strings:\n\t{Strings}", list2.Count, string.Join(",\n\t", list2));
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error occurred while loading strings");
			Save();
		}
		Traduction.AppliquerInterface();
		PostLoad();
	}

	private static void DeserializeDictionary<TKey>(List<string> missingStrings, Type groupType, FieldInfo fieldInfo, object? fieldValue, Dictionary<string, object> serializedGroup, object? serializedValue, Dictionary<TKey, LocalizedString> dictionary)
	{
		if (serializedValue != null)
		{
			if (!(serializedValue is JObject jObject))
			{
				return;
			}
			List<TKey> list = dictionary.Keys.ToList();
			{
				foreach (TKey item in list)
				{
					string propertyName = item?.ToString() ?? throw new InvalidOperationException($"{item}.{"ToString"}() returned null");
					if (!jObject.TryGetValue(propertyName, out JToken value) || value == null || value.Type != JTokenType.String)
					{
						missingStrings.Add($"{groupType.Name}.{fieldInfo.Name}[{item}]");
						jObject[propertyName] = (string)dictionary[item];
					}
					else
					{
						dictionary[item] = value.Value<string>();
					}
				}
				return;
			}
		}
		missingStrings.Add(groupType.Name + "." + fieldInfo.Name + " (string dictionary)");
		serializedGroup[fieldInfo.Name] = ((fieldValue == null) ? JValue.CreateNull() : JToken.FromObject(fieldValue));
	}

	private static Dictionary<string, string> SerializeDictionary<TKey>(Dictionary<TKey, LocalizedString> localizedDictionary) where TKey : notnull
	{
		return localizedDictionary.OrderBy<KeyValuePair<TKey, LocalizedString>, TKey>((KeyValuePair<TKey, LocalizedString> pair) => pair.Key, new OrdinalComparer<TKey>()).ToDictionary((KeyValuePair<TKey, LocalizedString> pair) => pair.Key.ToString() ?? throw new InvalidOperationException($"Failed to use {pair.Key} as a key"), (KeyValuePair<TKey, LocalizedString> pair) => pair.Value.ToString());
	}

	private static Dictionary<string, object> SerializeGroup(Type groupType)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (FieldInfo item in groupType.GetFields(BindingFlags.Static | BindingFlags.Public).OrderBy((FieldInfo f) => f.Name, new OrdinalComparer()))
		{
			object value = item.GetValue(null);
			if (!(value is LocalizedString localizedString))
			{
				if (!(value is Dictionary<int, LocalizedString> localizedDictionary))
				{
					if (!(value is Dictionary<string, LocalizedString> localizedDictionary2))
					{
						if (value is Dictionary<ChatboxTab, LocalizedString> localizedDictionary3)
						{
							dictionary.Add(item.Name, SerializeDictionary(localizedDictionary3));
							continue;
						}
						Type type = value?.GetType();
						if ((object)type != null && typeof(Dictionary<, >).ExtendedBy(type))
						{
							Type[] genericTypeArguments = type.GenericTypeArguments;
							Type type2 = genericTypeArguments.First();
							if (type2 == typeof(LocalizedString))
							{
								throw new InvalidOperationException("Expected a type that is not a " + typeof(LocalizedString).GetName(qualified: true));
							}
							MethodInfo methodInfo = _methodInfoSerializeDictionary.MakeGenericMethod(type2);
							object obj = methodInfo.Invoke(null, new object[1] { value });
							if (obj is Dictionary<string, string> value2)
							{
								dictionary.Add(item.Name, value2);
							}
						}
					}
					else
					{
						dictionary.Add(item.Name, SerializeDictionary(localizedDictionary2));
					}
				}
				else
				{
					dictionary.Add(item.Name, SerializeDictionary(localizedDictionary));
				}
			}
			else
			{
				dictionary.Add(item.Name, localizedString.ToString());
			}
		}
		return dictionary;
	}

	private static void SaveSerialized(Dictionary<string, Dictionary<string, object>> serialized)
	{
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory);
		if (Directory.Exists(text) && !Coffre.Actif)
		{
			File.WriteAllText(Path.Combine(text, "client_strings.json"), JsonConvert.SerializeObject(serialized, Formatting.Indented));
		}
	}

	public static void Save()
	{
		Dictionary<string, Dictionary<string, object>> dictionary = new Dictionary<string, Dictionary<string, object>>();
		Type typeFromHandle = typeof(Strings);
		Type[] nestedTypes = typeFromHandle.GetNestedTypes(BindingFlags.Static | BindingFlags.Public);
		foreach (Type item in nestedTypes.OrderBy((Type g) => g.Name, new OrdinalComparer()))
		{
			Dictionary<string, object> value = SerializeGroup(item);
			dictionary.Add(item.Name, value);
		}
		SaveSerialized(dictionary);
	}
}
