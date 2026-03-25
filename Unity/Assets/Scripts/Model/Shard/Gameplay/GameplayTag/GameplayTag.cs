using System;

namespace Bliss.Gameplay.GameplayTags
{
    /// <summary>
    /// A single gameplay tag, which represents a hierarchical name of the form x.y that is registered in the GameplayTagsManager
    /// You can filter the gameplay tags displayed in the editor using, meta = (Categories = "Tag1.Tag2.Tag3"))
    /// </summary>
    public struct GameplayTag: IEquatable<GameplayTag>
    {
        public static readonly GameplayTag EmptyTag = new GameplayTag(string.Empty);
        public string TagName { get; private set; }
        
        public GameplayTag(string tagName)
        {
            TagName = tagName;
        }
        
        public static bool operator ==(GameplayTag lhs, GameplayTag rhs)
        {
            return lhs.TagName == rhs.TagName;
        }

        public static bool operator !=(GameplayTag lhs, GameplayTag rhs)
        {
            return lhs.TagName != rhs.TagName;
        }

        /// <summary>
        /// Determine if this tag matches TagToCheck, expanding our parent tags
        /// "A.1".MatchesTag("A") will return True, "A".MatchesTag("A.1") will return False
        /// If TagToCheck is not Valid it will always return False
        /// </summary>
        /// <param name="tagToCheck"></param>
        /// <returns>True if this tag matches TagToCheck</returns>
        public bool MatchesTag(ref GameplayTag tagToCheck)
        {
            var tagContainer = GameplayTagsManager.Instance.GetSingleTagContainer(ref this);
            if (tagContainer != null)
            {
                return tagContainer.HasTag(ref tagToCheck);
            }

            if (IsValid())
            {
                GameplayTagsManager.Log($"{nameof(MatchesTag)} passed invalid gameplay tag {TagName}, only registered tags can be used in containers");
            }
            return false;
        }

        /// <summary>
        /// Determine if TagToCheck is valid and exactly matches this tag
        /// "A.1".MatchesTagExact("A") will return False
        /// If TagToCheck is not Valid it will always return False
        /// </summary>
        /// <param name="tagToCheck"></param>
        /// <returns>True if TagToCheck is Valid and is exactly this tag</returns>
        public bool MatchesTagExact(ref GameplayTag tagToCheck)
        {
            if (!tagToCheck.IsValid())
            {
                return false;
            }

            return TagName == tagToCheck.TagName;
        }

        /// <summary>
        /// Check to see how closely two FGameplayTags match. Higher values indicate more matching terms in the tags.
        /// </summary>
        /// <param name="tagToCheck"></param>
        /// <returns>The depth of the match, higher means they are closer to an exact match</returns>
        public int MatchesTagDepth(ref GameplayTag tagToCheck)
        {
            return GameplayTagsManager.Instance.GameplayTagsMatchDepth(ref tagToCheck, ref this);
        }
        
        
        /// <summary>
        /// Checks if this tag matches ANY of the tags in the specified container, also checks against our parent tags
        /// "A.1".MatchesAny({"A","B"}) will return True, "A".MatchesAny({"A.1","B"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return False
        /// </summary>
        /// <param name="containerToCheck"></param>
        /// <returns>True if this tag matches ANY of the tags of in ContainerToCheck</returns>
        public bool MatchesAny(GameplayTagContainer containerToCheck)
        {
            var tagContainer = GameplayTagsManager.Instance.GetSingleTagContainer(ref this);
            if (tagContainer != null)
            {
                return tagContainer.HasAny(containerToCheck);
            }

            if (IsValid())
            {
                GameplayTagsManager.Log($"{nameof(MatchesTag)} passed invalid gameplay tag {TagName}, only registered tags can be used in containers");
            }
            return false;
        }
        
        /// <summary>
        /// Checks if this tag matches ANY of the tags in the specified container, only allowing exact matches
        /// "A.1".MatchesAny({"A","B"}) will return False
        /// If ContainerToCheck is empty/invalid it will always return False 
        /// </summary>
        /// <param name="tagToCheck"></param>
        /// <returns>True if this tag matches ALL of the tags of in ContainerToCheck exactly</returns>
        public bool MatchesAnyExact(GameplayTagContainer tagToCheck)
        {
            if (tagToCheck.IsEmpty())
            {
                return false;
            }

            return tagToCheck.GameplayTags.Contains(this);
        }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(TagName);
        }

        /// <summary>
        /// Returns reference to a GameplayTagContainer containing only this tag
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetSingleTagContainer()
        {
            var tagContainer = GameplayTagsManager.Instance.GetSingleTagContainer(ref this);
            if (tagContainer != null)
            {
                return tagContainer;
            }

            return GameplayTagContainer.EmptyContainer;
        }
        
        /// <summary>
        /// Gets a Tag Container containing the supplied tag and all of it's parents as explicit tags
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetGameplayTagParents()
        {
            return GameplayTagsManager.Instance.RequestGameplayTagParents(ref this);
        }

        /// <summary>
        /// Returns direct parent GameplayTag of this GameplayTag, calling on x.y will return x
        /// </summary>
        /// <returns></returns>
        public GameplayTag RequestDirectParent()
        {
            return GameplayTagsManager.Instance.RequestGameplayTagDirectParent(ref this);
        }
        
        public override string ToString()
        {
            return TagName;
        }

        public override int GetHashCode()
        {
            return (TagName != null ? TagName.GetHashCode() : 0);
        }

        public bool Equals(GameplayTag other)
        {
            return TagName == other.TagName;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }
    }
}