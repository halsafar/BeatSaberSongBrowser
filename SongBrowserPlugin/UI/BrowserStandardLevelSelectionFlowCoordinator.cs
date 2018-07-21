using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRUI;

namespace SongBrowserPlugin.UI
{
    class BrowserStandardLevelSelectionFlowCoordinator : StandardLevelSelectionFlowCoordinator
    {
        public const String Name = "BrowserStandardLevelSelectionFlowCoordinator";
        private Logger _log = new Logger(Name);

        public virtual void Present(VRUIViewController parentViewController, IStandardLevel[] levels, GameplayMode gameplayMode)
        {
            _log.Debug("Present()");
            base.Present(parentViewController, levels, gameplayMode);
        }
    }
}
