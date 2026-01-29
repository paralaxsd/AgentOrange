using Spectre.Console;
using System.ComponentModel;

namespace AgentOrange
{
    sealed partial class AgentSkills
    {
        [Description("Verwendet AnsiConsole.Markup um Spectre.Console-Markup zu rendern.")]
        public void RenderSpectreMarkup(string markup) => AnsiConsole.Markup(markup);

        [Description("Verwendet AnsiConsole.MarkupLine um Spectre.Console-Markup zu rendern.")]
        public void RenderSpectreMarkupLine(string markup) => AnsiConsole.MarkupLine(markup);
    }
}
