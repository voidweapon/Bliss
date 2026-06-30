using UnityEngine;

namespace Bliss
{
    public static class GamePlayUtilities_Transform
    {
        public static Transform FindTransform(this Transform parent, string path, string sep)
        {
            var nodes = path.Split(sep);
            Transform node = parent;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (node == null) break;
                node = node.Find(nodes[i]);
            }

            return node;
        }
    }
}