using System.Collections.Generic;

namespace Bliss
{
	/// <summary>
	/// 管理Scene上的UI
	/// </summary>
	public class UIComponent: Entity, IAwake, IDestroy
	{
		public Dictionary<string, UI> UIs = new();

		private EntityRef<UIGlobalComponent> uiGlobalComponent;

		public UIGlobalComponent UIGlobalComponent
		{
			get
			{
				return (UIGlobalComponent)this.uiGlobalComponent;
			}
			set
			{
				this.uiGlobalComponent = value;
			}
		}
	}
}