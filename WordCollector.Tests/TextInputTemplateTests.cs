using System.Xml.Linq;

namespace WordCollector.Tests;

public class TextInputTemplateTests
{
    private static readonly XNamespace PresentationNamespace =
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace XamlNamespace =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    [Theory]
    [InlineData("TextBox")]
    [InlineData("PasswordBox")]
    public void InputTemplate_DoesNotApplyPaddingAgainToContentHost(string controlType)
    {
        var contentHost = FindContentHost(controlType);

        Assert.Null(contentHost.Attribute("Margin"));
        Assert.Null(contentHost.Parent?.Attribute("Padding"));
    }

    [Theory]
    [InlineData("TextBox")]
    [InlineData("PasswordBox")]
    public void InputTemplate_ContentHostCannotStealKeyboardFocus(string controlType)
    {
        var contentHost = FindContentHost(controlType);

        Assert.Equal("False", contentHost.Attribute("Focusable")?.Value);
    }

    private static XElement FindContentHost(string controlType)
    {
        var appXamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "WordCollector", "App.xaml"));
        var document = XDocument.Load(appXamlPath);
        var template = document
            .Descendants(PresentationNamespace + "ControlTemplate")
            .Single(element => element.Attribute("TargetType")?.Value == controlType);

        return template
            .Descendants(PresentationNamespace + "ScrollViewer")
            .Single(element => element.Attribute(XamlNamespace + "Name")?.Value == "PART_ContentHost");
    }
}
