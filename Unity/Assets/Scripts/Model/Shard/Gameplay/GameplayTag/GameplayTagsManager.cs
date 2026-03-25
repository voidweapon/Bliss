using System;
using System.Collections.Generic;
using System.Text;

namespace Bliss.Gameplay.GameplayTags
{
    public class GameplayTagsManager
    {
        private static GameplayTagsManager _instance;
        public static GameplayTagsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameplayTagsManager();
                }

                return _instance;
            }
        }

        private HashSet<string> Tags = new();

        /// <summary>
        /// Map of all config directories to load tag inis from
        /// </summary>
        private Dictionary<GameplayTag, GameplayTagNode> GameplayTagNodeMap = new Dictionary<GameplayTag, GameplayTagNode>();
        /// <summary>
        ///  Roots of gameplay tag nodes
        /// </summary>
        private GameplayTagNode GameplayRootTag;

        /// <summary>
        /// Sorted list of nodes, used for network replication
        /// </summary>
        private List<GameplayTagNode> NetworkGameplayTagNodeIndex;
        
        private uint NetworkGameplayTagNodeIndexHash;

        private const string C_Split = ".";

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string msg)
        {
            Bliss.Log.Debug(msg);
            //UnityEngine.Debug.Log(msg);
        }
        
        /// <summary>
        /// Helper function to get the stored TagContainer containing only this tag, which has searchable ParentTags
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns></returns>
        public GameplayTagContainer GetSingleTagContainer(ref GameplayTag gameplayTag)
        {
            GameplayTagNodeMap.TryGetValue(gameplayTag, out var Node);
            if (Node != null)
            {
                return Node.GetSingleTagContainer();
            }

            return null;
        }

        public void LoadGameplayTagsFrom(List<string> listTags)
        {
            listTags.Sort();
            foreach (var tag in listTags)
            {
                Tags.Add(tag);
            }

            DestroyGameplayTagTree();
            ConstructGameplayTagTree();
        }
        
        /// <summary>
        /// Loads the tag tables referenced in the GameplayTagSettings object
        /// </summary>
        void LoadGameplayTagTables()
        {
            
        }

        /// <summary>
        /// Helper function to construct the gameplay tag tree
        /// </summary>
        void ConstructGameplayTagTree()
        {
            if (GameplayRootTag == null)
            {
                GameplayRootTag = new GameplayTagNode();

                var strBuilder = new StringBuilder();
                foreach (var tag in Tags)
                {
                    GameplayTagNode curNode = GameplayRootTag;
                    strBuilder.Clear();
                    var subTags = tag.Split(C_Split);
                    for (int i = 0; i < subTags.Length; i++)
                    {
                        if (i == 0)
                        {
                            strBuilder.Append(subTags[i]);
                        }
                        else
                        {
                            strBuilder.Append(C_Split);
                            strBuilder.Append(subTags[i]);
                        }

                        var tempGameTag = new GameplayTag(strBuilder.ToString());
                        if (!GameplayTagNodeMap.TryGetValue(tempGameTag, out var subNode))
                        {
                            subNode = new GameplayTagNode(subTags[i], tempGameTag.TagName, curNode == GameplayRootTag ? null : curNode);
                            curNode.GetChildTagNodes().Add(subNode);
                            GameplayTagNodeMap.Add(tempGameTag, subNode);
                        }

                        curNode = subNode;
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to destroy the gameplay tag tree
        /// </summary>
        void DestroyGameplayTagTree()
        {
            if (GameplayRootTag != null)
            {
                GameplayRootTag.ResetNode();
                GameplayRootTag = null;
            }
            GameplayTagNodeMap.Clear();
        }

        /// <summary>
        /// Check to see how closely two FGameplayTags match. Higher values indicate more matching terms in the tags.
        /// </summary>
        /// <param name="GameplayTagOne"></param>
        /// <param name="GameplayTagTwo"></param>
        /// <returns>the length of the longest matching pair</returns>
        public int GameplayTagsMatchDepth(ref GameplayTag GameplayTagOne, ref GameplayTag GameplayTagTwo)
        {
            var tags1 = new HashSet<string>();
            var tags2 = new HashSet<string>();
            
            var TagNode = FindTagNode(ref GameplayTagOne);
            if (TagNode != null)
            {
                GetAllParentNodeNames(tags1, TagNode);
            }
            
            TagNode = FindTagNode(ref GameplayTagTwo);
            if (TagNode != null)
            {
                GetAllParentNodeNames(tags2, TagNode);
            }

            int intersectCount = 0;
            foreach (var tag in tags1)
            {
                if (tags2.Contains(tag))
                {
                    intersectCount++;
                }
            }
            
            return intersectCount;
        }

        /// <summary>
        /// Gets a Tag Container containing the supplied tag and all of it's parents as explicit tags
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns> A Tag Container with the supplied tag and all its parents added explicitly</returns>
        public GameplayTagContainer RequestGameplayTagParents(ref GameplayTag gameplayTag)
        {
            var singleContainer = GetSingleTagContainer(ref gameplayTag);
            if (singleContainer != null)
            {
                return singleContainer.GetGameplayTagParents();
            }

            return new GameplayTagContainer();
        }
        
        /// <summary>
        /// Returns direct parent GameplayTag of this GameplayTag, calling on x.y will return x
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns></returns>
        public GameplayTag RequestGameplayTagDirectParent(ref GameplayTag gameplayTag)
        {
            var gameplayNode = FindTagNode(ref gameplayTag);
            if (gameplayNode != null)
            {
                var parentNode = gameplayNode.GetParentTagNode();
                if (parentNode != null)
                {
                    return parentNode.GetCompleteTag();
                }
            }

            return GameplayTag.EmptyTag;
        }

        private static HashSet<string> MissingTagName;
        public GameplayTag RequestGameplayTag(string tagName, bool errorIfNotFound = false)
        {
            var possibleTag = new GameplayTag(tagName);
            if (GameplayTagNodeMap.ContainsKey(possibleTag))
            {
                return possibleTag;
            }
            else if(errorIfNotFound)
            {
                if (!MissingTagName.Contains(tagName))
                {
                    Log($"Requested Gameplay Tag {tagName} was not found, tags must be loaded from config or registered as a native tag");
                    MissingTagName.Add(tagName);
                }
            }
            
            return GameplayTag.EmptyTag;
        }

        /// <summary>
        /// Returns the number of parents a particular gameplay tag has.  Useful as a quick way to determine which tags may
        /// be more "specific" than other tags without comparing whether they are in the same hierarchy or anything else.
        /// Example: "TagA.SubTagA" has 2 Tag Nodes.  "TagA.SubTagA.LeafTagA" has 3 Tag Nodes.
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns></returns>
        int GetNumberOfTagNodes(ref GameplayTag gameplayTag)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Helper function for GameplayTagsMatch to get all parents when doing a parent match,
        /// NOTE: Must never be made public as it uses the FNames which should never be exposed
        /// </summary>
        /// <param name="nameLis"></param>
        /// <param name="gameplayTagNode"></param>
        void GetAllParentNodeNames(HashSet<string> nameLis,  GameplayTagNode gameplayTagNode)
        {
            nameLis.Add(gameplayTagNode.GetCompleteTagName());
            GameplayTagNode parentNode = gameplayTagNode.GetParentTagNode();
            if (parentNode != null)
            {
                GetAllParentNodeNames(nameLis, parentNode);
            }
        }
        
        
        
        /// <summary>
        ///  Checks node tree to see if a FGameplayTagNode with the tag exists
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns></returns>
        GameplayTagNode FindTagNode(ref GameplayTag gameplayTag)
        {
            GameplayTagNodeMap.TryGetValue(gameplayTag, out var node);
            if (node != null)
            {
                return node;
            }

            return null;
        }
    }
}