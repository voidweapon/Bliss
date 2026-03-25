namespace Bliss.Gameplay.GameplayTags
{
    /// <summary>
    /// Interface for assets which contain gameplay tags
    /// </summary>
    public interface IGameplayTagAsset
    {
        /// <summary>
        /// Get any owned gameplay tags on the asset
        /// </summary>
        /// <param name="tagContainer"></param>
        void GetOwnedGameplayTags(GameplayTagContainer tagContainer);

        /// <summary>
        /// Check if the asset has a gameplay tag that matches against the specified tag (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tagToCheck">Tag to check for a match</param>
        /// <returns>True if the asset has a gameplay tag that matches, false if not</returns>
        bool HasMatchingGameplayTag(GameplayTag tagToCheck);
        
        /// <summary>
        ///  Check if the asset has gameplay tags that matches against all the specified tags (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tagContainer">Tag container to check for a match</param>
        /// <returns>True if the asset has matches all the gameplay tags, will be true if container is empty</returns>
        bool HasAllMatchingGameplayTags(GameplayTagContainer tagContainer);
        
        /// <summary>
        /// Check if the asset has gameplay tags that matches against any of the specified tags (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tagContainer">Tag container to check for a match</param>
        /// <returns>True if the asset has matches any of the gameplay tags, will be false if container is empty</returns>
        bool HasAnyMatchingGameplayTags(GameplayTagContainer  tagContainer);
    }
}