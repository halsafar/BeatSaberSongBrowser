using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongBrowserPlugin.Tests
{
    public class SongBrowserTestRunner
    {
        private Logger _log = new Logger("SongBrowserTestRunner");

        public SongBrowserTestRunner()
        {

        }

        public void RunTests()
        {
            _log.Info("Running Song Browser Tests");

            new SongBrowserModelTests().RunTest();
        }
    }
}
