using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongBrowserPlugin.DataAccess.FileSystem
{
    class DirectoryNode
    {
        public string Key { get; private set; }
        public Dictionary<String, DirectoryNode> Nodes;
        public List<LevelSO> Levels;

        public DirectoryNode(String key)
        {
            Key = key;
            Nodes = new Dictionary<string, DirectoryNode>();
            Levels = new List<LevelSO>();
        }
    }
}
