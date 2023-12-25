using Godot;
using System;
using GodotInk;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

enum EntryType
{
    Default,
    Player,
    Character
}

public class LogEntry
{
    public string TitleText { get; set; }
    public string BodyText { get; set; }

    public Color TitleColor { get; set; }
    public Color BodyColor { get; set; }

    public bool Active { get; set; } = true;

    public LogEntry(string titleText = "", string bodyText = "", Color titleColor = new Color(), Color bodyColor = new Color())
    {
        TitleText = titleText;
        BodyText = bodyText;
        TitleColor = titleColor;
        BodyColor = bodyColor;
    }

    public virtual string PrepareBBCode(float darkening = 0.0f)
    {
        var text = "";

        var color = TitleColor;

        if (TitleText != "")
        {

            text += string.Format("[color={0}]", color.Darkened(darkening).ToHtml());
            text += string.Format("[b]{0}[/b]", TitleText.ToUpper());
            text += " - [/color]";
        }

        color = BodyColor;

        text += string.Format("[color={0}]{1}[/color]", color.Darkened(darkening).ToHtml(), BodyText);
        return text;
    }
}

public class LogEntryChoice : LogEntry
{
    public LogEntryChoice(string titleText = "", string bodyText = "", Color titleColor = new Color())
    {
        TitleText = titleText;
        BodyText = bodyText;
        TitleColor = titleColor;
    }

    public override string PrepareBBCode(float darkening = 0.0f)
    {
        return BodyText;
    }
}

public class LogSection
{
    readonly List<LogEntry> logEntries = new();

    public void SetActive(bool active = true)
    {
        foreach (var logEntry in logEntries)
            logEntry.Active = active;
    }

    public string PrepareBBCode()
    {
        var text = "";

        foreach (var logEntry in logEntries)
            text += logEntry.PrepareBBCode(logEntry.Active ? 0.0f : 0.3f);

        return text;
    }

    public void AddEntry(LogEntry logEntry)
    {
        logEntries.Add(logEntry);
    }
}

public class LogManager
{
    List<LogSection> logSections = new();

    public void AddEntry(LogEntry logEntry)
    {
        logSections.Last().AddEntry(logEntry);
    }

    public void AddSection(LogSection logSection)
    {
        logSections.Add(logSection);
    }

    public void Update()
    {
        for (int i = 0; i < logSections.Count; i++)
        {
            LogSection logSection = logSections[i];

            if (i < logSections.Count - 1)
                logSection.SetActive(false);
        }
    }

    public string GetBBCode()
    {
        string bbCode = "";

        foreach (var logSection in logSections)
            bbCode += logSection.PrepareBBCode();

        return bbCode;
    }

    internal void RemoveLastSection()
    {
        logSections.RemoveAt(logSections.Count - 1);
    }
}

public partial class TextPanel : RichTextLabel
{
    [Export]
    InkStory story;

    [Signal]
    public delegate void StoryFinishedEventHandler();

    [Export]
    float inactiveDarkening = 0.3f;

    [Export]
    Godot.Collections.Dictionary<string, Color> logEntryColors = new()
    {
        ["default_title"] = Colors.Wheat,
        ["default_body"] = Colors.White,
        ["player_title"] = Colors.Pink,
        ["player_body"] = Colors.White,
        ["character_title"] = Colors.Blue,
        ["character_body"] = Colors.White,
        ["choice"] = Colors.Red,
    };

    readonly List<string> visitedChoices = new();

    LogManager logManager;

    string characterName;
    Color characterTitleColor;
    Color characterBodyColor;

    public override void _Ready()
    {
        base._Ready();

        Init();
    }

    public void Init()
    {
        MetaClicked += GetChoice;

        characterName = story.FetchVariable<string>("character_name");

        var characterTitleColorString = story.FetchVariable<string>("character_title_color");
        var characterBodyColorString = story.FetchVariable<string>("character_body_color");

        if (characterTitleColorString != "")
            characterTitleColor = Color.FromHtml(characterTitleColorString);

        if (characterBodyColorString != "")
            characterBodyColor = Color.FromHtml(characterTitleColorString);

        logManager = new();

        logManager.AddSection(PrepareText());

        PrintStep();
    }

    private void PrintStep()
    {
        logManager.Update();

        logManager.AddSection(PrepareChoices());

        var textBuffer = logManager.GetBBCode();

        ParseBbcode(textBuffer);
    }

    private LogSection PrepareText()
    {
        LogSection logSection = new();

        EntryType lastEntryType = EntryType.Default;

        while (story.CanContinue)
        {
            string storyText = story.Continue();

            if (storyText.StripEdges() != string.Empty)
            {
                LogEntry logEntry = new();

                if (story.CurrentTags.Contains("Player"))
                {
                    // color = Player.DialogColor;
                    logEntry.TitleColor = logEntryColors["player_title"];
                    logEntry.BodyColor = logEntryColors["player_body"];

                    if (lastEntryType != EntryType.Player)
                        logEntry.TitleText = "You";

                    lastEntryType = EntryType.Player;
                }
                else if (story.CurrentTags.Contains("Character"))
                {
                    // color = Character.DialogColor;
                    logEntry.TitleColor = characterTitleColor;
                    logEntry.BodyColor = characterBodyColor;

                    if (lastEntryType != EntryType.Character)
                        logEntry.TitleText = characterName;

                    lastEntryType = EntryType.Character;
                }
                else
                {

                    logEntry.TitleColor = logEntryColors["default_title"];
                    logEntry.BodyColor = logEntryColors["default_body"];
                    lastEntryType = EntryType.Default;
                }

                logEntry.BodyText = storyText + "\n";

                logSection.AddEntry(logEntry);
            }
        }

        return logSection;
    }

    private LogSection PrepareChoices()
    {
        LogSection logSection = new();

        if (story.CurrentChoices.Count > 0)
        {
            var listContent = "";

            foreach (InkChoice choice in story.CurrentChoices)
            {
                var color = logEntryColors["choice"];

                // if (Story.VisitCountAtPathString(choice.PathStringOnChoice) > 0)
                if (visitedChoices.Contains(choice.PathStringOnChoice))
                    color = color.Darkened(inactiveDarkening);

                listContent += " - ";
                listContent += string.Format("[color={0}]", color.ToHtml());
                listContent += string.Format("[url={0}]{1}[/url]", choice.Index, choice.Text);
                listContent += "[/color]\n";

                // GD.Print(choice.PathStringOnChoice);
            }

            logSection.AddEntry(new LogEntryChoice("", string.Format("[ol]{0}[/ol]", listContent)));
        }
        else
            EmitSignal(SignalName.StoryFinished);

        return logSection;
    }

    private LogSection PrepareChoice(string text)
    {
        LogSection logSection = new();

        logSection.AddEntry(new LogEntry("You", text + "\n\n", logEntryColors["player_title"], logEntryColors["player_body"]));

        return logSection;
    }

    public void GetChoice(Variant meta)
    {
        var choice = story.CurrentChoices[(int)meta];
        var choiceText = choice.Text;
        story.ChooseChoiceIndex((int)meta);

        // GD.Print("Choice:");
        // // GD.Print(choice.SourcePath);
        // GD.Print(choice.PathStringOnChoice);
        // GD.Print(Story.VisitCountAtPathString(choice.PathStringOnChoice));

        visitedChoices.Add(choice.PathStringOnChoice);

        logManager.RemoveLastSection();

        logManager.AddSection(PrepareChoice(choiceText));
        logManager.AddSection(PrepareText());

        PrintStep();
    }
}
