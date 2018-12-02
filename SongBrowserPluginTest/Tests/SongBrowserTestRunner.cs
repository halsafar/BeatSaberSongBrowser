using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPluginTests
{
    public class SongBrowserTestRunner
    {
        public SongBrowserTestRunner()
        {

        }

        public void RunTests()
        {
            Logger.Info("Running Song Browser Tests");

            new SongBrowserModelTests().RunTest();
        }
    }
}
