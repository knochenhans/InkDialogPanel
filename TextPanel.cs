using Godot;
using System;
using GodotInk;
using System.Collections.Generic;

enum entryType
{
    Action,
    Player,
    Character
}

public class LogEntry
{
    public string titleText;
    public string bodyText;
    public Color titleColor;
    public Color bodyColor = Colors.White;
    public bool active = true;

    float darkening = 0.3f;

    public LogEntry(string titleText_ = "", string bodyText_ = "", Color titleColor_ = new Color())
    {
        titleText = titleText_;
        bodyText = bodyText_;
        titleColor = titleColor_;
    }

    public virtual string PrepareBBCode()
    {
        var text = "";

        var color = titleColor;

        if (titleText != "")
        {
            if (!active)
                color = color.Darkened(darkening);

            text += String.Format("[color={0}]", color.ToHtml());
            text += String.Format("[b]{0}[/b]", titleText.ToUpper());
            text += " - [/color]";
        }

        color = bodyColor;

        if (!active)
            color = color.Darkened(darkening);

        text += String.Format("[color={0}]{1}[/color]", color.ToHtml(), bodyText);
        return text;
    }
}

public class LogEntryChoice : LogEntry
{
    public LogEntryChoice(string titleText_ = "", string bodyText_ = "", Color titleColor_ = new Color())
    {
        titleText = titleText_;
        bodyText = bodyText_;
        titleColor = titleColor_;
    }

    public override string PrepareBBCode()
    {
        return bodyText;
    }
}

public class LogSection
{
    public List<LogEntry> logEntries = new();

    public void SetActive(bool active = true)
    {
        foreach (var logEntry in logEntries)
            logEntry.active = active;
    }

    public string PrepareBBCode()
    {
        var text = "";

        foreach (var logEntry in logEntries)
            text += logEntry.PrepareBBCode();

        return text;
    }
}

public partial class TextPanel : RichTextLabel
{
    [Export]
    InkStory Story;

    [Signal]
    public delegate void StoryFinishedEventHandler();

    // private InkStory Story;

    // private Player Player;
    // private Character Character;

    int addedChoiceParagraphCount = 0;
    int addedTextParagraphCount = 0;
    string lastParagraph;

    // string textBuffer;
    // string choicesText;

    List<LogSection> logSections = new();
    List<string> visitedChoices = new();

    float darkening = 0.3f;

    string name;

    public override void _Ready()
    {
        base._Ready();

        Init();
    }

    // public void Init(InkStory story, Player player, Character character)
    public void Init()
    {
        // Story = new InkStory();

        // Player = player;
        // Character = character;

        MetaClicked += GetChoice;

        name = Story.FetchVariable<string>("name");

        logSections.Add(PrepareText());

        PrintStep();
    }

    private void UpdateLog()
    {
        for (int i = 0; i < logSections.Count; i++)
        {
            LogSection logSection = logSections[i];

            if (i < logSections.Count - 1)
                logSection.SetActive(false);
        }
    }

    private void PrintStep()
    {
        UpdateLog();

        logSections.Add(PrepareChoices());

        var textBuffer = "";

        foreach (var logSection in logSections)
        {
            textBuffer += logSection.PrepareBBCode();
        }

        ParseBbcode(textBuffer);
    }

    private LogSection PrepareText()
    {
        LogSection logSection = new();

        entryType lastEntryType = entryType.Action;

        while (Story.CanContinue)
        {
            string storyText = Story.Continue();

            if (storyText.StripEdges() != string.Empty)
            {
                LogEntry logEntry = new();

                if (Story.CurrentTags.Contains("Action"))
                {
                    logEntry.titleColor = Colors.Gray;
                    lastEntryType = entryType.Action;
                }
                else if (Story.CurrentTags.Contains("Player"))
                {
                    // color = Player.DialogColor;
                    logEntry.titleColor = Colors.White;

                    if (lastEntryType != entryType.Player)
                        logEntry.titleText = "You";

                    lastEntryType = entryType.Player;
                }
                else if (Story.CurrentTags.Contains("Character"))
                {
                    // color = Character.DialogColor;
                    logEntry.titleColor = Colors.Wheat;
                    logEntry.bodyColor = Colors.Wheat;

                    if (lastEntryType != entryType.Character)
                        logEntry.titleText = name;

                    lastEntryType = entryType.Character;
                }

                logEntry.bodyText = storyText + "\n";

                logSection.logEntries.Add(logEntry);
            }
        }

        return logSection;
    }

    private LogSection PrepareChoices()
    {
        LogSection logSection = new();

        if (Story.CurrentChoices.Count > 0)
        {
            var listContent = "";

            foreach (InkChoice choice in Story.CurrentChoices)
            {
                var color = Colors.Red;

                // if (Story.VisitCountAtPathString(choice.PathStringOnChoice) > 0)
                if (visitedChoices.Contains(choice.PathStringOnChoice))
                    color = color.Darkened(darkening);

                listContent += " - ";
                listContent += String.Format("[color={0}]", color.ToHtml());
                listContent += String.Format("[url={0}]{1}[/url]", choice.Index, choice.Text);
                listContent += "[/color]\n";

                // GD.Print(choice.PathStringOnChoice);
            }

            logSection.logEntries.Add(new LogEntryChoice("", String.Format("[ol]{0}[/ol]", listContent)));
        }
        else
        {
            // Story finished
            EmitSignal(SignalName.StoryFinished);
        }

        return logSection;
    }

    private LogSection PrepareChoice(string text)
    {
        LogSection logSection = new();

        logSection.logEntries.Add(new LogEntry("You", text + "\n\n", Colors.Pink));

        return logSection;
    }

    public void GetChoice(Variant meta)
    {
        var choice = Story.CurrentChoices[(int)meta];
        var choiceText = choice.Text;
        Story.ChooseChoiceIndex((int)meta);

        // GD.Print("Choice:");
        // // GD.Print(choice.SourcePath);
        // GD.Print(choice.PathStringOnChoice);
        // GD.Print(Story.VisitCountAtPathString(choice.PathStringOnChoice));

        visitedChoices.Add(choice.PathStringOnChoice);

        logSections.RemoveAt(logSections.Count - 1);
        logSections.Add(PrepareChoice(choiceText));
        logSections.Add(PrepareText());

        PrintStep();
    }

    private void RemoveParagraphs(int count)
    {
        GD.Print(String.Format("Remove {0}", count));
        for (int i = 0; i < count - 1; i++)
            RemoveParagraph(GetParagraphCount() - 1);
    }
}
