using System;
using System.Collections;
using System.Collections.Generic;

namespace Bliss.Gameplay.GameplayTags
{
    /// <summary>
    /// A Tag Container holds a collection of GameplayTags, tags are included explicitly by adding them, and implicitly from adding child tags
    /// </summary>
    public class GameplayTagContainer : IEnumerable<GameplayTag>
    {
        public GameplayTagContainer()
        {
            
        }
        
        public GameplayTagContainer(ref GameplayTag tag)
        {
            AddTag(ref tag);
        }
        
        public GameplayTagContainer(GameplayTagContainer other)
        {
            GameplayTags.AddRange(other.GameplayTags);
            ParentTags.AddRange(other.ParentTags);
        }

        public static GameplayTagContainer EmptyContainer = new GameplayTagContainer();
        
        internal List<GameplayTag> GameplayTags = new List<GameplayTag>();
        
        /// <summary>
        /// Array of expanded parent tags, in addition to GameplayTags. Used to accelerate parent searches. May contain duplicates in some cases
        /// </summary>
        internal List<GameplayTag> ParentTags = new List<GameplayTag>();

        /// <summary>
        /// Determine if TagToCheck is present in this container, also checking against parent tags
        /// {"A.1"}.HasTag("A") will return True, {"A"}.HasTag("A.1") will return False
        /// If TagToCheck is not Valid it will always return False
        /// </summary>
        /// <param name="TagToCheck"></param>
        /// <returns>True if TagToCheck is in this container, false if it is not</returns>
        public bool HasTag(ref GameplayTag TagToCheck)
        {
            if (!TagToCheck.IsValid())
            {
                return false;
            }

            return GameplayTags.Contains(TagToCheck) || ParentTags.Contains(TagToCheck);
        }

        /// <summary>
        /// Determine if TagToCheck is explicitly present in this container, only allowing exact matches
        /// {"A.1"}.HasTagExact("A") will return False
        /// If TagToCheck is not Valid it will always return False
        /// </summary>
        /// <param name="TagToCheck"></param>
        /// <returns>True if TagToCheck is in this container, false if it is not</returns>
        public bool HasTagExact(ref GameplayTag TagToCheck)
        {
            if (!TagToCheck.IsValid())
            {
                return false;
            }

            return GameplayTags.Contains(TagToCheck);
        }
        
        /// <summary>
        /// Checks if this container contains ANY of the tags in the specified container, also checks against parent tags
        /// {"A.1"}.HasAny({"A","B"}) will return True, {"A"}.HasAny({"A.1","B"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return False 
        /// </summary>
        /// <param name="ContainerToCheck"></param>
        /// <returns>True if this container has ANY of the tags of in ContainerToCheck</returns>
        public bool HasAny(GameplayTagContainer ContainerToCheck)
        {
            if (ContainerToCheck.IsEmpty())
            {
                return false;
            }

            foreach (var otherTag in ContainerToCheck.GameplayTags)
            {
                if (GameplayTags.Contains(otherTag) || ParentTags.Contains(otherTag))
                {
                    return true;
                }
            }

            return false;
        }
        
        
        /// <summary>
        /// Checks if this container contains ANY of the tags in the specified container, only allowing exact matches
        /// {"A.1"}.HasAnyExact({"A","B"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return False
        /// </summary>
        /// <param name="ContainerToCheck"></param>
        /// <returns>True if this container has ANY of the tags of in ContainerToCheck</returns>
        public bool HasAnyExact(GameplayTagContainer ContainerToCheck)
        {
            if (ContainerToCheck.IsEmpty())
            {
                return false;
            }

            foreach (var otherTag in ContainerToCheck.GameplayTags)
            {
                if (GameplayTags.Contains(otherTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this container contains ALL of the tags in the specified container, also checks against parent tags
        /// {"A.1","B.1"}.HasAll({"A","B"}) will return True, {"A","B"}.HasAll({"A.1","B.1"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
        /// </summary>
        /// <param name="ContainerToCheck"></param>
        /// <returns></returns>
        public bool HasAll(GameplayTagContainer ContainerToCheck)
        {
            if (ContainerToCheck.IsEmpty())
            {
                return true;
            }

            foreach (var otherTag in ContainerToCheck.GameplayTags)
            {
                if (!GameplayTags.Contains(otherTag) && !ParentTags.Contains(otherTag))
                {
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Checks if this container contains ALL of the tags in the specified container, only allowing exact matches
        /// {"A.1","B.1"}.HasAll({"A","B"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
        /// </summary>
        /// <param name="ContainerToCheck"></param>
        /// <returns>True if this container has ALL of the tags of in ContainerToCheck, including if ContainerToCheck is empty</returns>
        public bool HasAllExact(GameplayTagContainer ContainerToCheck)
        {
            if (ContainerToCheck.IsEmpty())
            {
                return true;
            }

            foreach (var otherTag in ContainerToCheck.GameplayTags)
            {
                if (!GameplayTags.Contains(otherTag))
                {
                    return false;
                }
            }

            return true;
        }
        
        public int Num()
        {
            return GameplayTags.Count;
        }
        
        public bool IsValid()
        {
            return GameplayTags.Count > 0;
        }
        
        public bool IsEmpty()
        {
            return GameplayTags.Count == 0;
        }

        /// <summary>
        /// Returns a new container explicitly containing the tags of this container and all of their parent tags
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetGameplayTagParents()
        {
            var resultContainer = new GameplayTagContainer();
            resultContainer.GameplayTags.AddRange(GameplayTags);
            foreach (var parentTag in ParentTags)
            {
                if (!resultContainer.GameplayTags.Contains(parentTag))
                {
                    resultContainer.GameplayTags.Add(parentTag);
                }
            }

            return resultContainer;
        }
        
        /// <summary>
        /// Returns a filtered version of this container, returns all tags that match against any of the tags in OtherContainer, expanding parents
        /// </summary>
        /// <param name="OtherContainer"></param>
        /// <returns></returns>
        public GameplayTagContainer Filter(GameplayTagContainer OtherContainer)
        {
            GameplayTagContainer ResultContainer = new GameplayTagContainer();
            for (int i = 0; i < GameplayTags.Count; i++)
            {
                var tag = GameplayTags[i];
                if (tag.MatchesAny(OtherContainer))
                {
                    ResultContainer.AddTagFast(ref tag);
                }
            }

            return ResultContainer;
        }
        
        /// <summary>
        /// Returns a filtered version of this container, returns all tags that match exactly one in OtherContainer
        /// </summary>
        /// <param name="OtherContainer"></param>
        /// <returns></returns>
        public GameplayTagContainer FilterExact(GameplayTagContainer OtherContainer)
        {
            GameplayTagContainer ResultContainer = new GameplayTagContainer();
            for (int i = 0; i < GameplayTags.Count; i++)
            {
                var tag = GameplayTags[i];
                if (tag.MatchesAnyExact(OtherContainer))
                {
                    ResultContainer.AddTagFast(ref tag);
                }
            }

            return ResultContainer;
        }

        /// <summary>
        ///  Checks if this container matches the given query.
        /// </summary>
        /// <param name="Query"></param>
        /// <returns>True if this container matches the query, false otherwise.</returns>
        public bool MatchesQuery(GameplayTagQuery Query)
        {
            return Query.Matches(this);
        }
        
        /// <summary>
        /// Adds all the tags from one container to this container 
        /// NOTE: From set theory, this effectively is the union of the container this is called on with Other.
        /// </summary>
        /// <param name="Other">TagContainer that has the tags you want to add to this container </param>
        public void AppendTags(GameplayTagContainer Other)
        {
            if(Other == null) return;
            
            foreach (var otherTag in Other.GameplayTags)
            {
                if (!GameplayTags.Contains(otherTag))
                {
                    GameplayTags.Add(otherTag);
                }
            }
            
            foreach (var otherTag in Other.ParentTags)
            {
                if (!ParentTags.Contains(otherTag))
                {
                    ParentTags.Add(otherTag);
                }
            }
        }
        
        /// <summary>
        /// Adds all the tags that match between the two specified containers to this container.  WARNING: This matches any
        /// parent tag in A, not just exact matches!  So while this should be the union of the container this is called on with
        /// the intersection of OtherA and OtherB, it's not exactly that.  Since OtherB matches against its parents, any tag
        /// in OtherA which has a parent match with a parent of OtherB will count.  For example, if OtherA has Color.Green
        /// and OtherB has Color.Red, that will count as a match due to the Color parent match!
        /// If you want an exact match, you need to call A.FilterExact(B) (above) to get the intersection of A with B.
        /// If you need the disjunctive union (the union of two sets minus their intersection), use AppendTags to create
        /// Union, FilterExact to create Intersection, and then call Union.RemoveTags(Intersection). 
        /// </summary>
        /// <param name="OtherA">TagContainer that has the matching tags you want to add to this container, these tags have their parents expanded</param>
        /// <param name="OtherB">TagContainer used to check for matching tags.  If the tag matches on any parent, it counts as a match.</param>
        public void AppendMatchingTags(GameplayTagContainer OtherA, GameplayTagContainer OtherB)
        {
            for (int i = 0; i < OtherA.GameplayTags.Count; i++)
            {
                var otherATag = OtherA.GameplayTags[i];
                if (otherATag.MatchesAny(OtherB))
                {
                    AddTag(ref otherATag);
                }
            }
        }
        
        /// <summary>
        /// Add the specified tag to the container
        /// </summary>
        /// <param name="tagToAdd"></param>
        public void AddTag(ref GameplayTag tagToAdd)
        {
            if (tagToAdd.IsValid())
            {
                GameplayTags.Add(tagToAdd);
                
                AddParentsForTag(ref tagToAdd);
            }
        }
        
        /// <summary>
        /// Add the specified tag to the container without checking for uniqueness
        /// Useful when building container from another data struct
        /// </summary>
        /// <param name="tagToAdd"></param>
        /// <returns></returns>
        public void AddTagFast(ref GameplayTag tagToAdd)
        {
            GameplayTags.Add(tagToAdd);
                
            AddParentsForTag(ref tagToAdd);
        }

        /// <summary>
        /// Adds a tag to the container and removes any direct parents, wont add if child already exists
        /// </summary>
        /// <param name="TagToAdd"></param>
        /// <returns>True if tag was added</returns>
        public bool AddLeafTag(ref GameplayTag TagToAdd)
        {
            if (HasTagExact(ref TagToAdd))
            {
                return true;
            }

            if (HasTag(ref TagToAdd))
            {
                return false;
            }

            var TagToAddContainer = GameplayTagsManager.Instance.GetSingleTagContainer(ref TagToAdd);
            if (TagToAddContainer == null)
            {
                return false;
            }

            int removeCount = 0;
            for (int i = 0; i < TagToAddContainer.ParentTags.Count; i++)
            {
                var ParentTag = TagToAddContainer.ParentTags[i];
                if (HasTagExact(ref ParentTag))
                {
                    RemoveTag(ref ParentTag, true);
                    removeCount++;
                }
            }
            
            if(removeCount > 0)
            {
                FillParentTags();
            }

            AddTag(ref  TagToAdd);
            return true;
        }
        
        /// <summary>
        /// Tag to remove from the container
        /// </summary>
        /// <param name="TagToRemove">Tag to remove from the container</param>
        /// <param name="bDeferParentTags">Skip calling FillParentTags for performance (must be handled by calling code)</param>
        /// <returns></returns>
        public bool RemoveTag(ref GameplayTag TagToRemove, bool bDeferParentTags = false)
        {
            bool result = GameplayTags.Remove(TagToRemove);
            if (result)
            {
                if (!bDeferParentTags)
                {
                    FillParentTags();
                }

                return true;
            }

            return false;
        }
        
        /// <summary>
        ///  Removes all tags in TagsToRemove from this container
        /// </summary>
        /// <param name="TagsToRemove"></param>
        public void RemoveTags(GameplayTagContainer TagsToRemove)
        {
            int NumChanged = 0;
            foreach (var tag in TagsToRemove.GameplayTags)
            {
                if (GameplayTags.Remove(tag))
                {
                    NumChanged++;
                }
            }

            if (NumChanged > 0)
            {
                FillParentTags();
            }
        }

        /// <summary>
        ///  Remove all tags from the container.
        /// </summary>
        public void Reset()
        {
            GameplayTags.Clear();
            ParentTags.Clear();
        }


        /// <summary>
        ///  Fills in ParentTags from GameplayTags
        /// </summary>
        public void FillParentTags()
        {
            ParentTags.Clear();
            
            for (int i = 0; i < GameplayTags.Count; i++)
            {
                var tag = GameplayTags[i];
                AddParentsForTag(ref tag);
            }
        }
        
        /// <summary>
        /// Adds parent tags for a single tag
        /// </summary>
        /// <param name="tag"></param>
        public void AddParentsForTag(ref GameplayTag tag)
        {
            var singleContainer = GameplayTagsManager.Instance.GetSingleTagContainer(ref tag);
            if (singleContainer != null)
            {
                foreach (var parentTag in singleContainer.ParentTags)
                {
                    if (!ParentTags.Contains(parentTag))
                    {
                        ParentTags.Add(parentTag);
                    }
                }
            }
            
        }

        public IEnumerator<GameplayTag> GetEnumerator()
        {
            return GameplayTags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GameplayTags.GetEnumerator();
        }
    }
}