using Godot;
using System;
using GodotInk;
using System.Collections.Generic;
using System.Linq;

enum EntryType
{
    Action,
    Player,
    Character
}

public class LogEntry
{
    public string TitleText { get; set; }
    public string BodyText { get; set; }

    public Color TitleColor { get; set; }
    public Color BodyColor { get; set; } = Colors.White;

    public bool Active { get; set; } = true;

    float Darkening { get; set; }

    public LogEntry(string titleText = "", string bodyText = "", Color titleColor = new Color(), float darkening = 0.3f)
    {
        TitleText = titleText;
        BodyText = bodyText;
        TitleColor = titleColor;
        Darkening = darkening;
    }

    public virtual string PrepareBBCode()
    {
        var text = "";

        var color = TitleColor;

        if (TitleText != "")
        {
            if (!Active)
                color = color.Darkened(Darkening);

            text += string.Format("[color={0}]", color.ToHtml());
            text += string.Format("[b]{0}[/b]", TitleText.ToUpper());
            text += " - [/color]";
        }

        color = BodyColor;

        if (!Active)
            color = color.Darkened(Darkening);

        text += string.Format("[color={0}]{1}[/color]", color.ToHtml(), BodyText);
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

    public override string PrepareBBCode()
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
            text += logEntry.PrepareBBCode();

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

    string Name { get; set; }
    Color CharacterColor { get; set; }
    float Darkening { get; set; }

    public LogManager(string name, Color characterColor, float darkening)
    {
        Name = name;
        CharacterColor = characterColor;
        Darkening = darkening;
    }

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
    Color actionTextTitleColor = Colors.White;

    [Export]
    Color actionTextBodyColor = Colors.Wheat;

    [Export]
    Color playerTextTitleColor = Colors.White;

    [Export]
    Color playerTextBodyColor = Colors.Wheat;

    [Export]
    float darkening = 0.3f;

    readonly List<string> visitedChoices = new();

    LogManager logManager;

    string characterName;

    public override void _Ready()
    {
        base._Ready();

        Init();
    }

    public void Init()
    {
        MetaClicked += GetChoice;

        characterName = story.FetchVariable<string>("name");
        var characterColor = Color.FromHtml(story.FetchVariable<string>("color"));

        logManager = new(characterName, characterColor, darkening);

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

        EntryType lastEntryType = EntryType.Action;

        while (story.CanContinue)
        {
            string storyText = story.Continue();

            if (storyText.StripEdges() != string.Empty)
            {
                LogEntry logEntry = new();

                if (story.CurrentTags.Contains("Action"))
                {
                    logEntry.TitleColor = Colors.Gray;
                    lastEntryType = EntryType.Action;
                }
                else if (story.CurrentTags.Contains("Player"))
                {
                    // color = Player.DialogColor;
                    logEntry.TitleColor = Colors.White;

                    if (lastEntryType != EntryType.Player)
                        logEntry.TitleText = "You";

                    lastEntryType = EntryType.Player;
                }
                else if (story.CurrentTags.Contains("Character"))
                {
                    // color = Character.DialogColor;
                    logEntry.TitleColor = Colors.Wheat;
                    logEntry.BodyColor = Colors.Wheat;

                    if (lastEntryType != EntryType.Character)
                        logEntry.TitleText = characterName;

                    lastEntryType = EntryType.Character;
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
                var color = Colors.Red;

                // if (Story.VisitCountAtPathString(choice.PathStringOnChoice) > 0)
                if (visitedChoices.Contains(choice.PathStringOnChoice))
                    color = color.Darkened(darkening);

                listContent += " - ";
                listContent += string.Format("[color={0}]", color.ToHtml());
                listContent += string.Format("[url={0}]{1}[/url]", choice.Index, choice.Text);
                listContent += "[/color]\n";

                // GD.Print(choice.PathStringOnChoice);
            }

            logSection.AddEntry(new LogEntryChoice("", string.Format("[ol]{0}[/ol]", listContent)));
        }
        else
        {
            EmitSignal(SignalName.StoryFinished);
        }

        return logSection;
    }

    private LogSection PrepareChoice(string text)
    {
        LogSection logSection = new();

        logSection.AddEntry(new LogEntry("You", text + "\n\n", Colors.Pink));

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
