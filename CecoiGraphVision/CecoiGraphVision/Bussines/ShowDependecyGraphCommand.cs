using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using System.IO;

namespace CecoiGraphVision
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ShowDependencyGraphCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command set GUID.
        /// </summary>
        public static readonly Guid CommandSet = new Guid("ba2a0b54-c42c-4e78-a01a-939217e3a576");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowDependencyGraphCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the .vsct file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ShowDependencyGraphCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ShowDependencyGraphCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ShowDependencyGraphCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new ShowDependencyGraphCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get the DTE service
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            if (dte == null)
            {
                throw new InvalidOperationException("Unable to get DTE service.");
            }

            // Scan the solution
            var scanner = new SolutionScanner(dte);
            var projectInfos = await scanner.ScanSolutionAsync();

            // Generate the graph
            var visualizer = new GraphVisualizer();
            var outputPath = Path.Combine(Path.GetTempPath(), "dependencyGraph.dot");
            visualizer.GenerateGraph(projectInfos, outputPath);

            // Find and show the tool window
            var window = (CecoiGraphWindow)package.FindToolWindow(typeof(CecoiGraphWindow), 0, true);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException("Cannot create tool window.");
            }

            var control = (CecoiGraphWindowControl)window.Content;
            control.DisplayGraph(outputPath);
        }
    }
}
