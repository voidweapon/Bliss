using System;
using Unity.Mathematics;

namespace Bliss.EditorTool.LevelEditorTool
{
    public class ShooterGearEditorActor : AbstractLevelEditorActor
    {
        public ShooterGearConfig Config =  new ShooterGearConfig();


        protected override Type GetActorConfigType()
        {
           return typeof(ShooterGearConfig);
        }

        protected override byte[] OnSerialize()
        {
            SetupConfigTransform(out Config.Position, out Config.Rotation, out var _);
            return Serializer.Serialize(Config);
        }

        protected override void OnDeserialize(byte[] data)
        {
            Config = Serializer.Deserialize(typeof(ShooterGearConfig), data, 0, data.Length) as ShooterGearConfig;
            if (Config != null) 
                SetupTransformFromConfig(Config.Position, Config.Rotation, new float3(1, 1, 1));
        }
    }
}