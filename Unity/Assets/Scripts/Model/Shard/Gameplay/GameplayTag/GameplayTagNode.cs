using System;
using System.Collections.Generic;

namespace Bliss.Gameplay.GameplayTags
{
    internal class GameplayTagNode
    {
        public GameplayTagNode()
        {
            CompleteTagWithParents = new GameplayTagContainer();
        }

        public GameplayTagNode(string tagName, string fullTagName, GameplayTagNode parentNode)
        {
            ParentNode = parentNode;
            Tag = tagName;
            CompleteTagWithParents = new GameplayTagContainer();
            
            CompleteTagWithParents.GameplayTags.Add(new GameplayTag(fullTagName));

            if (parentNode != null &&  !string.IsNullOrEmpty(parentNode.Tag))
            {
                var parentContainer = parentNode.GetSingleTagContainer();
                CompleteTagWithParents.ParentTags.Add(parentContainer.GameplayTags[0]);
                CompleteTagWithParents.ParentTags.AddRange(parentContainer.ParentTags);
            }
        }
        /// <summary>
        /// Child gameplay tag nodes
        /// </summary>
        private List<GameplayTagNode> ChildTags = new List<GameplayTagNode>();
        
        /// <summary>
        /// Owner gameplay tag node, if any
        /// </summary>
        private GameplayTagNode ParentNode;

        /// <summary>
        /// Raw name for this tag at current rank in the tree
        /// </summary>
        private string Tag;

        /// <summary>
        /// This complete tag is at GameplayTags[0], with parents in ParentTags[]
        /// </summary>
        private GameplayTagContainer CompleteTagWithParents;

        /// <summary>
        /// Returns a correctly constructed container with only this tag, useful for doing container queries
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetSingleTagContainer()
        {
            return CompleteTagWithParents;
        }

        /// <summary>
        ///  Get the complete tag for the node, including all parent tags, delimited by periods
        /// </summary>
        /// <returns></returns>
        public GameplayTag GetCompleteTag()
        {
            return CompleteTagWithParents.Num() > 0 ? CompleteTagWithParents.GameplayTags[0] : GameplayTag.EmptyTag;
        }

        public string GetCompleteTagName()
        {
            return GetCompleteTag().TagName;
        }
        
        /// <summary>
        /// Get the children nodes of this node
        /// </summary>
        /// <returns></returns>
        public List<GameplayTagNode> GetChildTagNodes()
        {
            return ChildTags;
        }
        
        /// <summary>
        /// Get the parent tag node of this node
        /// </summary>
        /// <returns></returns>
        public GameplayTagNode GetParentTagNode()
        {
            return ParentNode;
        }
        
        public void ResetNode()
        {
            Tag = String.Empty;
            CompleteTagWithParents.Reset();
            
            foreach (var childTag in ChildTags)
            {
                childTag.ResetNode();
            }
            ChildTags.Clear();
            ParentNode = null;
        }
    }
}