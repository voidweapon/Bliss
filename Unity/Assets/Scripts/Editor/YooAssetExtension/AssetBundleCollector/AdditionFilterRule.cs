using System.IO;
using YooAsset.Editor;

namespace YooAssetExtension
{
    [DisplayName("收集除场景外的所有资源")]
    public class CollectAllButScene : IFilterRule
    {
        public bool IsCollectAsset(FilterRuleData data)
        {
            string extension = Path.GetExtension(data.AssetPath);
            return extension != ".unity" && extension != ".scene";
        }

        public string FindAssetType       
        {
            get { return EAssetSearchType.All.ToString(); }
        }
    }
}