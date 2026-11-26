using System;
using Unity.Mathematics;

namespace Bliss.EditorTool.LevelEditorTool
{
    public class PlayerSpawnPointEditorActor : AbstractLevelEditorActor
    {
        public PlayerSpawnPointConfig Config =  new PlayerSpawnPointConfig();
        protected override Type GetActorConfigType()
        {
            return typeof(PlayerSpawnPointConfig);
        }

        protected override byte[] OnSerialize()
        {
            SetupConfigTransform(out Config.Position, out Config.Rotation, out var _);
            return Serializer.Serialize(Config);
        }

        protected override void OnDeserialize(byte[] data)
        {
            Config = Serializer.Deserialize(typeof(PlayerSpawnPointConfig), data, 0, data.Length) as PlayerSpawnPointConfig;
            if (Config != null) 
                SetupTransformFromConfig(Config.Position, Config.Rotation, new float3(1, 1, 1));
        }
    }
}