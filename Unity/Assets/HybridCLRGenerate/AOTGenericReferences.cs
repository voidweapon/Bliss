using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Bliss.Base.dll",
		"Bliss.Core.dll",
		"Bliss.ThirdParty.dll",
		"Bliss.Unity.dll",
		"Luban.Runtime.dll",
		"MemoryPack.dll",
		"System.Core.dll",
		"System.Runtime.CompilerServices.Unsafe.dll",
		"System.dll",
		"Unity.Cinemachine.dll",
		"Unity.InputSystem.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Bliss.AEvent<object,Bliss.Event_AfterCreateGameplayWorld>
	// Bliss.AEvent<object,Bliss.Event_AfterCreateLevelScene>
	// Bliss.AEvent<object,Bliss.Event_AfterCreateSceneActor>
	// Bliss.AEvent<object,Bliss.Event_EndChangeLevelScene>
	// Bliss.AEvent<object,Bliss.Event_EndCreateLevelScene>
	// Bliss.AEvent<object,Bliss.Event_InitProcessAppInitFinish>
	// Bliss.AEvent<object,Bliss.Event_InitProcessEntry>
	// Bliss.AEvent<object,Bliss.Event_InitProcessEntry_Demo>
	// Bliss.AEvent<object,Bliss.Event_LoginSuccess>
	// Bliss.AEvent<object,Bliss.Event_OnAcceiptConnection>
	// Bliss.AEvent<object,Bliss.Event_StartChangeLevelScene>
	// Bliss.AEvent<object,Bliss.Event_SyncViewTransform>
	// Bliss.AEvent<object,Bliss.Event_TransformChange>
	// Bliss.AEvent<object,Bliss.GameplayEvent_ShootBullet>
	// Bliss.AInvokeHandler<Bliss.AddForceAtPositionParams>
	// Bliss.AInvokeHandler<Bliss.ConfigManager.LoadFile,object>
	// Bliss.AInvokeHandler<Bliss.GameplayBoxColliderAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplayBoxColliderDestroyParams>
	// Bliss.AInvokeHandler<Bliss.GameplayCapsuleColliderAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplayCapsuleColliderDestroyParams>
	// Bliss.AInvokeHandler<Bliss.GameplayHeightFieldColliderAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplayHeightFieldDestroyParams>
	// Bliss.AInvokeHandler<Bliss.GameplayMeshColliderAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplayMeshColliderDestroyParams>
	// Bliss.AInvokeHandler<Bliss.GameplayRigidbodyAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplayRigidbodyDestroyParams>
	// Bliss.AInvokeHandler<Bliss.GameplaySphereColliderAwakeParams>
	// Bliss.AInvokeHandler<Bliss.GameplaySphereColliderDestroyParams>
	// Bliss.AInvokeHandler<Bliss.Invoke_OnReceiveMessage>
	// Bliss.AInvokeHandler<Bliss.Invoke_RootInit,object>
	// Bliss.AInvokeHandler<Bliss.MovePositionAndRotationParams>
	// Bliss.AInvokeHandler<Bliss.MovePositionParams>
	// Bliss.AInvokeHandler<Bliss.MoveRotationParams>
	// Bliss.AInvokeHandler<Bliss.Physics.Invoke_PhysicalSceneAwake>
	// Bliss.AInvokeHandler<Bliss.Physics.Invoke_PhysicalSceneDestroy>
	// Bliss.AInvokeHandler<Bliss.Physics.Invoke_SpereCastNonAlloc,int>
	// Bliss.AInvokeHandler<Bliss.SetAngularVelocityParams>
	// Bliss.AInvokeHandler<Bliss.SetBoxColliderCenterParams>
	// Bliss.AInvokeHandler<Bliss.SetBoxColliderSizeParams>
	// Bliss.AInvokeHandler<Bliss.SetCapsuleColliderCenterParams>
	// Bliss.AInvokeHandler<Bliss.SetCapsuleColliderDirectionParams>
	// Bliss.AInvokeHandler<Bliss.SetCapsuleColliderHeightParams>
	// Bliss.AInvokeHandler<Bliss.SetCapsuleColliderRadiusParams>
	// Bliss.AInvokeHandler<Bliss.SetCenterOfMassParams>
	// Bliss.AInvokeHandler<Bliss.SetMassParams>
	// Bliss.AInvokeHandler<Bliss.SetPositionParams>
	// Bliss.AInvokeHandler<Bliss.SetRotationParams>
	// Bliss.AInvokeHandler<Bliss.SetSphereColliderCenterParams>
	// Bliss.AInvokeHandler<Bliss.SetSphereColliderRadiusParams>
	// Bliss.AInvokeHandler<Bliss.SetVelocityParams>
	// Bliss.AInvokeHandler<Bliss.Timer.TimerCallback>
	// Bliss.AInvokeHandler<Bliss.WakeUpParams>
	// Bliss.AwakeSystem<object,Bliss.BoxColliderInitParams>
	// Bliss.AwakeSystem<object,Bliss.BulletCreateInfo>
	// Bliss.AwakeSystem<object,Bliss.CapsuleColliderInitParams>
	// Bliss.AwakeSystem<object,Bliss.CharacterMovement.KinematicMoverManagerSettings>
	// Bliss.AwakeSystem<object,Bliss.GameplayHeightFieldColliderInitParams>
	// Bliss.AwakeSystem<object,Bliss.GameplayMeshColliderInitParams>
	// Bliss.AwakeSystem<object,Bliss.GameplayRigidbodyInitParams>
	// Bliss.AwakeSystem<object,Bliss.NetRoleComponentInitParam>
	// Bliss.AwakeSystem<object,Bliss.NetworkClientParam>
	// Bliss.AwakeSystem<object,Bliss.NetworkServerParam>
	// Bliss.AwakeSystem<object,Bliss.PeriodicMotionComponent.InitParam>
	// Bliss.AwakeSystem<object,Bliss.SmoothTransformComponentInitParam>
	// Bliss.AwakeSystem<object,Bliss.SphereColliderInitParams>
	// Bliss.AwakeSystem<object,Bliss.UIAwakeInfo>
	// Bliss.AwakeSystem<object,Bliss.ViewModel.GameObjectViewComponentInitParam>
	// Bliss.AwakeSystem<object,int>
	// Bliss.AwakeSystem<object,object>
	// Bliss.AwakeSystem<object>
	// Bliss.Base.Collections.ArrayComponent<Bliss.GameplayRaycastHit>
	// Bliss.Base.Collections.DoublyDictionary<object,object>
	// Bliss.Base.Collections.FixedCircularArray<Bliss.GameplayInputActionStatus>
	// Bliss.Base.Collections.FixedCircularArray<object>
	// Bliss.Base.Collections.IndexList<long,Bliss.EntityRef<object>>
	// Bliss.Base.Collections.ListComponent<object>
	// Bliss.Base.Event.AutoEventAction<Bliss.Gameplay.GameplayAbilitySystem.AbilityEndedData>
	// Bliss.Base.Event.AutoEventAction<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// Bliss.Base.Event.AutoEventAction<object,object>
	// Bliss.Base.Event.AutoEventAction<object>
	// Bliss.Base.SubClassOf<object>
	// Bliss.DestroySystem<object>
	// Bliss.EarlyUpdateSystem<object>
	// Bliss.EntityRef.EqualityComparer<object>
	// Bliss.EntityRef<object>
	// Bliss.IAwake<Bliss.BoxColliderInitParams>
	// Bliss.IAwake<Bliss.BulletCreateInfo>
	// Bliss.IAwake<Bliss.CapsuleColliderInitParams>
	// Bliss.IAwake<Bliss.CharacterMovement.KinematicMoverManagerSettings>
	// Bliss.IAwake<Bliss.GameplayHeightFieldColliderInitParams>
	// Bliss.IAwake<Bliss.GameplayMeshColliderInitParams>
	// Bliss.IAwake<Bliss.GameplayRigidbodyInitParams>
	// Bliss.IAwake<Bliss.NetRoleComponentInitParam>
	// Bliss.IAwake<Bliss.NetworkClientParam>
	// Bliss.IAwake<Bliss.NetworkServerParam>
	// Bliss.IAwake<Bliss.PeriodicMotionComponent.InitParam>
	// Bliss.IAwake<Bliss.SmoothTransformComponentInitParam>
	// Bliss.IAwake<Bliss.SphereColliderInitParams>
	// Bliss.IAwake<Bliss.UIAwakeInfo>
	// Bliss.IAwake<Bliss.ViewModel.GameObjectViewComponentInitParam>
	// Bliss.IAwake<int>
	// Bliss.IAwake<object>
	// Bliss.IAwakeSystem<Bliss.BoxColliderInitParams>
	// Bliss.IAwakeSystem<Bliss.BulletCreateInfo>
	// Bliss.IAwakeSystem<Bliss.CapsuleColliderInitParams>
	// Bliss.IAwakeSystem<Bliss.CharacterMovement.KinematicMoverManagerSettings>
	// Bliss.IAwakeSystem<Bliss.GameplayHeightFieldColliderInitParams>
	// Bliss.IAwakeSystem<Bliss.GameplayMeshColliderInitParams>
	// Bliss.IAwakeSystem<Bliss.GameplayRigidbodyInitParams>
	// Bliss.IAwakeSystem<Bliss.NetRoleComponentInitParam>
	// Bliss.IAwakeSystem<Bliss.NetworkClientParam>
	// Bliss.IAwakeSystem<Bliss.NetworkServerParam>
	// Bliss.IAwakeSystem<Bliss.PeriodicMotionComponent.InitParam>
	// Bliss.IAwakeSystem<Bliss.SmoothTransformComponentInitParam>
	// Bliss.IAwakeSystem<Bliss.SphereColliderInitParams>
	// Bliss.IAwakeSystem<Bliss.UIAwakeInfo>
	// Bliss.IAwakeSystem<Bliss.ViewModel.GameObjectViewComponentInitParam>
	// Bliss.IAwakeSystem<int>
	// Bliss.IAwakeSystem<object>
	// Bliss.ISingletonInit<object>
	// Bliss.LateUpdateSystem<object>
	// Bliss.MessageHandler<object,object>
	// Bliss.MountToSceneSystem<object>
	// Bliss.Singleton<object>
	// Bliss.UpdateSystem<object>
	// DataStructures.SortedCollections.SortedList<object>
	// DataStructures.Trees.BSTNode<object>
	// DataStructures.Trees.BinarySearchTree.BinarySearchTreeInOrderEnumerator<object>
	// DataStructures.Trees.BinarySearchTree.BinarySearchTreePostOrderEnumerator<object>
	// DataStructures.Trees.BinarySearchTree.BinarySearchTreePreOrderEnumerator<object>
	// DataStructures.Trees.BinarySearchTree<object>
	// DataStructures.Trees.RedBlackTree<object>
	// DataStructures.Trees.RedBlackTreeNode<object>
	// ET.ETAsyncTaskMethodBuilder<Bliss.Wait_AfterCreateGameObjectView>
	// ET.ETAsyncTaskMethodBuilder<Bliss.Wait_CreateLevelScene>
	// ET.ETAsyncTaskMethodBuilder<Bliss.Wait_CreateMyUnit>
	// ET.ETAsyncTaskMethodBuilder<Bliss.Wait_FinishChangeLevelScene>
	// ET.ETAsyncTaskMethodBuilder<Bliss.Wait_LoadLevelSceneView>
	// ET.ETAsyncTaskMethodBuilder<byte>
	// ET.ETAsyncTaskMethodBuilder<object>
	// ET.ETTask<Bliss.Wait_AfterCreateGameObjectView>
	// ET.ETTask<Bliss.Wait_CreateLevelScene>
	// ET.ETTask<Bliss.Wait_CreateMyUnit>
	// ET.ETTask<Bliss.Wait_FinishChangeLevelScene>
	// ET.ETTask<Bliss.Wait_LoadLevelSceneView>
	// ET.ETTask<byte>
	// ET.ETTask<int>
	// ET.ETTask<object>
	// ET.StateMachineWrap<Bliss.AUGUI_UIEvent.<OnCreate>d__1>
	// ET.StateMachineWrap<Bliss.ActorCreateHelper.<CreateTestActor>d__0>
	// ET.StateMachineWrap<Bliss.C2G_EnterLevel_Handler.<Run>d__0>
	// ET.StateMachineWrap<Bliss.CameraComponentSystems.<SetupPlayerVirtualCameraWaitTargetViewLoad>d__7>
	// ET.StateMachineWrap<Bliss.CameraComponentSystems.<WaitTargetViewLoaded>d__8>
	// ET.StateMachineWrap<Bliss.Entry.<StartAsync>d__1>
	// ET.StateMachineWrap<Bliss.EventHander_InitProcessEntryDemo.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHander_LoginSucess_UI.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHander_StartChangeLevelScene_UI.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandle_Level_AfterCreateSceneActor.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_AfterCreateGameplayWorld.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_EndChangeLevelScene.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_EndCreateLevelScene.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_Level_SyncViewTransform.<Run>d__0>
	// ET.StateMachineWrap<Bliss.EventHandler_StartChangeLevelScene.<Run>d__0>
	// ET.StateMachineWrap<Bliss.LevelSceneFactory.<Create>d__0>
	// ET.StateMachineWrap<Bliss.Logic.Client.EventHandler_OnAcceiptConnection_TestServer.<Run>d__0>
	// ET.StateMachineWrap<Bliss.Logic.InovkeHandler_RootInit_Main_Client.<Handle>d__0>
	// ET.StateMachineWrap<Bliss.LoginHelper.<Login>d__0>
	// ET.StateMachineWrap<Bliss.MapHelper.<ChangeMap>d__1>
	// ET.StateMachineWrap<Bliss.MapHelper.<EnterMap>d__0>
	// ET.StateMachineWrap<Bliss.MessageHabdler_TransformSimpleSyncMessage.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_ChangeMap.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_CreateBullets.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_CreateMyUnit.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_CreateSceneActors.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_CreateUnits.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0>
	// ET.StateMachineWrap<Bliss.MessageHandler_PropertySync_LongMessage.<Run>d__0>
	// ET.StateMachineWrap<Bliss.ObjectWaitSystems.<>c__DisplayClass3_0.<<Wait>g__WaitTimeout|0>d<object>>
	// ET.StateMachineWrap<Bliss.ObjectWaitSystems.<Wait>d__2<object>>
	// ET.StateMachineWrap<Bliss.ObjectWaitSystems.<Wait>d__3<object>>
	// ET.StateMachineWrap<Bliss.PingMessageHandler.<Run>d__0>
	// ET.StateMachineWrap<Bliss.PongMessageHandler.<Run>d__0>
	// ET.StateMachineWrap<Bliss.SessionMessageHandler.<HandleAsync>d__3<object,object>>
	// ET.StateMachineWrap<Bliss.SessionMessageHandler.<HandleAsync>d__3<object>>
	// ET.StateMachineWrap<Bliss.SessionSystem.<>c__DisplayClass8_0.<<Call>g__Timeout|1>d>
	// ET.StateMachineWrap<Bliss.SessionSystem.<Call>d__7>
	// ET.StateMachineWrap<Bliss.SessionSystem.<Call>d__8>
	// ET.StateMachineWrap<Bliss.UIComponentSystem.<Create>d__2>
	// ET.StateMachineWrap<Bliss.UIGlobalComponentSystem.<OnCreate>d__1>
	// ET.StateMachineWrap<Bliss.UILoadingEvent.<AfterCreate>d__1>
	// ET.StateMachineWrap<Bliss.UILoginEvent.<AfterCreate>d__1>
	// ET.StateMachineWrap<Bliss.UIMapSelectComponentSystems.<EnterMap>d__3>
	// ET.StateMachineWrap<Bliss.UIMapSelectEvent.<AfterCreate>d__1>
	// ET.StateMachineWrap<Bliss.ViewLogic.Debugger.TransformStatDebugComponentSystems.<InitRes>d__6>
	// ET.StateMachineWrap<Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0>
	// ET.StateMachineWrap<Bliss.ViewLogic.Event_InitProcessEntry_InitClientGlobal.<Run>d__0>
	// ET.StateMachineWrap<Bliss.ViewLogic.GameObjectViewComponentSystems.<LoadViewmodel>d__4>
	// ET.StateMachineWrap<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>>
	// ET.StateMachineWrap<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>>
	// ET.StateMachineWrap<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7>
	// ET.StateMachineWrap<Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0>
	// ET.StateMachineWrap<Bliss.ViewLogic.Tests.EventHandler_SyncViewTransform.<Run>d__0>
	// MemoryPack.Formatters.ArrayFormatter<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>
	// MemoryPack.Formatters.ArrayFormatter<byte>
	// MemoryPack.Formatters.ArrayFormatter<object>
	// MemoryPack.Formatters.ListFormatter<long>
	// MemoryPack.Formatters.ListFormatter<object>
	// MemoryPack.Formatters.UnmanagedFormatter<int>
	// MemoryPack.IMemoryPackFormatter<long>
	// MemoryPack.IMemoryPackFormatter<object>
	// MemoryPack.IMemoryPackable<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>
	// MemoryPack.IMemoryPackable<object>
	// MemoryPack.MemoryPackFormatter<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>
	// MemoryPack.MemoryPackFormatter<int>
	// MemoryPack.MemoryPackFormatter<object>
	// System.Action<Bliss.Debugger.GameDebugStats.DebugStatGroup.DrawInfo>
	// System.Action<Bliss.EntityRef<object>>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.AbilityEndedData>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Action<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Action<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Action<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Action<Bliss.GameplayInputActionStatus>
	// System.Action<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Action<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Action<System.ValueTuple<float,float>>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<byte>
	// System.Action<int,object>
	// System.Action<int>
	// System.Action<long>
	// System.Action<object,long>
	// System.Action<object,object,object,object,object>
	// System.Action<object,object>
	// System.Action<object>
	// System.Action<uint,Bliss.Core.SocketBufferRef>
	// System.Action<uint,int>
	// System.Action<uint,object>
	// System.ByReference<byte>
	// System.Collections.Concurrent.ConcurrentDictionary.<GetEnumerator>d__35<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.DictionaryEnumerator<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Node<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary.Tables<object,object>
	// System.Collections.Concurrent.ConcurrentDictionary<object,object>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<Bliss.MainThreadOperations>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<Bliss.NetThreadOperation>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<object>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<Bliss.MainThreadOperations>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<Bliss.NetThreadOperation>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<object>
	// System.Collections.Concurrent.ConcurrentQueue<Bliss.MainThreadOperations>
	// System.Collections.Concurrent.ConcurrentQueue<Bliss.NetThreadOperation>
	// System.Collections.Concurrent.ConcurrentQueue<object>
	// System.Collections.Generic.ArraySortHelper<Bliss.EntityRef<object>>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.ArraySortHelper<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.ArraySortHelper<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.ArraySortHelper<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<float,float>>
	// System.Collections.Generic.ArraySortHelper<byte>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.Comparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.Comparer<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.Comparer<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.Comparer<System.ValueTuple<float,float>>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<float>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Comparer<ushort>
	// System.Collections.Generic.ComparisonComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.ComparisonComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.ComparisonComparer<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.ComparisonComparer<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ComparisonComparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ComparisonComparer<System.ValueTuple<float,float>>
	// System.Collections.Generic.ComparisonComparer<byte>
	// System.Collections.Generic.ComparisonComparer<float>
	// System.Collections.Generic.ComparisonComparer<int>
	// System.Collections.Generic.ComparisonComparer<long>
	// System.Collections.Generic.ComparisonComparer<object>
	// System.Collections.Generic.ComparisonComparer<ushort>
	// System.Collections.Generic.Dictionary.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary.Enumerator<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary.Enumerator<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary.Enumerator<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary.Enumerator<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary.KeyCollection<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary.KeyCollection<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary.KeyCollection<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary.KeyCollection<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary.KeyCollection<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary.KeyCollection<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,long>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary.KeyCollection<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,long>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary.ValueCollection<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary.ValueCollection<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary.ValueCollection<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary.ValueCollection<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary.ValueCollection<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary.ValueCollection<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,long>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary.ValueCollection<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.Dictionary<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.Dictionary<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.Dictionary<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.Dictionary<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.Dictionary<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.Dictionary<int,Bliss.RpcInfo>
	// System.Collections.Generic.Dictionary<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.Dictionary<int,Unity.Mathematics.float3>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,long>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.Dictionary<long,int>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.EqualityComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.EqualityComparer<Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.EqualityComparer<Bliss.RpcInfo>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<int,int,object>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<int,object>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<object,long>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.EqualityComparer<Unity.Mathematics.float3>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<ushort>
	// System.Collections.Generic.HashSet.Enumerator<Bliss.EntityRef<object>>
	// System.Collections.Generic.HashSet.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<Bliss.EntityRef<object>>
	// System.Collections.Generic.HashSet<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.HashSetEqualityComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<Bliss.EntityRef<object>>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.ICollection<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.ICollection<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.ICollection<Bliss.MainThreadOperations>
	// System.Collections.Generic.ICollection<Bliss.NetThreadOperation>
	// System.Collections.Generic.ICollection<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int,object>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,object>,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.ValueTuple<object,long>,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,Bliss.RpcInfo>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,System.ValueTuple<object,object>>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,Unity.Mathematics.float3>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,Bliss.EntityRef<object>>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.ValueTuple<float,float>>
	// System.Collections.Generic.ICollection<byte>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.IComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.IComparer<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.IComparer<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IComparer<System.ValueTuple<float,float>>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<Bliss.EntityRef<object>>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.IEnumerable<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.IEnumerable<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.IEnumerable<Bliss.MainThreadOperations>
	// System.Collections.Generic.IEnumerable<Bliss.NetThreadOperation>
	// System.Collections.Generic.IEnumerable<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int,object>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,object>,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.ValueTuple<object,long>,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,Bliss.RpcInfo>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,System.ValueTuple<object,object>>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,Unity.Mathematics.float3>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,Bliss.EntityRef<object>>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<float,float>>
	// System.Collections.Generic.IEnumerable<byte>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<Bliss.EntityRef<object>>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.IEnumerator<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.IEnumerator<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.IEnumerator<Bliss.MainThreadOperations>
	// System.Collections.Generic.IEnumerator<Bliss.NetThreadOperation>
	// System.Collections.Generic.IEnumerator<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int,object>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<int,object>,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.ValueTuple<object,long>,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,Bliss.RpcInfo>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,System.ValueTuple<object,object>>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,Unity.Mathematics.float3>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,long>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,Bliss.EntityRef<object>>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<float,float>>
	// System.Collections.Generic.IEnumerator<byte>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.IEqualityComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<int,int,object>>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<int,object>>
	// System.Collections.Generic.IEqualityComparer<System.ValueTuple<object,long>>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<Bliss.EntityRef<object>>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.IList<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.IList<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.IList<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IList<System.ValueTuple<float,float>>
	// System.Collections.Generic.IList<byte>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,float>
	// System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,int>
	// System.Collections.Generic.KeyValuePair<Bliss.Gameplay.GameplayTags.GameplayTag,object>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<int,int,object>,object>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<int,object>,float>
	// System.Collections.Generic.KeyValuePair<System.ValueTuple<object,long>,object>
	// System.Collections.Generic.KeyValuePair<int,Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.KeyValuePair<int,Bliss.RpcInfo>
	// System.Collections.Generic.KeyValuePair<int,System.ValueTuple<object,object>>
	// System.Collections.Generic.KeyValuePair<int,Unity.Mathematics.float3>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,long>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,Bliss.EntityRef<object>>
	// System.Collections.Generic.KeyValuePair<long,int>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<Bliss.EntityRef<object>>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.List.Enumerator<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.List.Enumerator<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.List.Enumerator<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<float,float>>
	// System.Collections.Generic.List.Enumerator<byte>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<Bliss.EntityRef<object>>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.List<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.List<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.List<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.List<System.ValueTuple<float,float>>
	// System.Collections.Generic.List<byte>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.Generic.ObjectComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.ObjectComparer<Bliss.GameplayInputActionStatus>
	// System.Collections.Generic.ObjectComparer<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<float,float>>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<float>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectComparer<ushort>
	// System.Collections.Generic.ObjectEqualityComparer<Bliss.EntityRef<object>>
	// System.Collections.Generic.ObjectEqualityComparer<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.Generic.ObjectEqualityComparer<Bliss.PeriodicMotionComponentSystems.TransformSyncMode>
	// System.Collections.Generic.ObjectEqualityComparer<Bliss.RpcInfo>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<int,int,object>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<int,object>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<object,long>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.ObjectEqualityComparer<Unity.Mathematics.float3>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<ushort>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_0<long,object>
	// System.Collections.Generic.SortedDictionary.<>c__DisplayClass34_1<long,object>
	// System.Collections.Generic.SortedDictionary.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass5_0<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.<>c__DisplayClass6_0<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.KeyCollection<long,object>
	// System.Collections.Generic.SortedDictionary.KeyValuePairComparer<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass5_0<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.<>c__DisplayClass6_0<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.SortedDictionary.ValueCollection<long,object>
	// System.Collections.Generic.SortedDictionary<long,object>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass52_0<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.<>c__DisplayClass53_0<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.Enumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.Node<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.TreeSubSet.<>c__DisplayClass9_0<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet.TreeSubSet<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.SortedSet<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.Generic.TreeSet<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.TreeWalkPredicate<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.EntityRef<object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.GameplayInputActionStatus>
	// System.Collections.ObjectModel.ReadOnlyCollection<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<float,float>>
	// System.Collections.ObjectModel.ReadOnlyCollection<byte>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<Bliss.EntityRef<object>>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Comparison<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Comparison<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Comparison<Bliss.GameplayInputActionStatus>
	// System.Comparison<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Comparison<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Comparison<System.ValueTuple<float,float>>
	// System.Comparison<byte>
	// System.Comparison<float>
	// System.Comparison<int>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Comparison<ushort>
	// System.Dynamic.Utils.CacheDict.Entry<object,object>
	// System.Dynamic.Utils.CacheDict<object,object>
	// System.Func<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffect,byte>
	// System.Func<object,Unity.Mathematics.float3>
	// System.Func<object,byte>
	// System.Func<object,long>
	// System.Func<object,object,byte,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IComparable<object>
	// System.IEquatable<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.IEquatable<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.IEquatable<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.IEquatable<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilityTargetDataHandle>
	// System.IEquatable<Bliss.Gameplay.GameplayAbilitySystem.PredictionKey>
	// System.IEquatable<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>
	// System.IEquatable<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Linq.Buffer<object>
	// System.Linq.Expressions.Expression<object>
	// System.Nullable<Bliss.Core.SocketBufferRef>
	// System.Nullable<Bliss.Gameplay.GameplayAbilitySystem.GameplayEventData>
	// System.Nullable<Bliss.PeriodicMotionComponent.SavedMove>
	// System.Predicate<Bliss.EntityRef<object>>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.AbilitySystemComponent.AbilityListLockActiveChange>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.AbilityTriggerData>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayCue>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.ActiveGameplayEffectHandle>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpec>
	// System.Predicate<Bliss.Gameplay.GameplayAbilitySystem.GameplayAbilitySpecHandle>
	// System.Predicate<Bliss.Gameplay.GameplayTags.GameplayTag>
	// System.Predicate<Bliss.GameplayInputActionStatus>
	// System.Predicate<Bliss.SessionMessageDispatcher.MessageSessionDispatcherInfo>
	// System.Predicate<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Predicate<System.ValueTuple<float,float>>
	// System.Predicate<byte>
	// System.Predicate<int>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlySpan<byte>
	// System.Runtime.CompilerServices.ReadOnlyCollectionBuilder.Enumerator<object>
	// System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<object>
	// System.Runtime.CompilerServices.TrueReadOnlyCollection<object>
	// System.Span<byte>
	// System.ValueTuple<float,float>
	// System.ValueTuple<int,int,object>
	// System.ValueTuple<int,object>
	// System.ValueTuple<object,long>
	// System.ValueTuple<object,object>
	// System.ValueTuple<ushort,object>
	// UnityEngine.InputSystem.InputBindingComposite<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputProcessor<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.InlinedArray<object>
	// }}

	public void RefMethods()
	{
		// object Bliss.Entity.AddChild<object,Bliss.BoxColliderInitParams>(Bliss.BoxColliderInitParams,bool)
		// object Bliss.Entity.AddChild<object,Bliss.CapsuleColliderInitParams>(Bliss.CapsuleColliderInitParams,bool)
		// object Bliss.Entity.AddChild<object,Bliss.GameplayHeightFieldColliderInitParams>(Bliss.GameplayHeightFieldColliderInitParams,bool)
		// object Bliss.Entity.AddChild<object,Bliss.GameplayMeshColliderInitParams>(Bliss.GameplayMeshColliderInitParams,bool)
		// object Bliss.Entity.AddChild<object,Bliss.SphereColliderInitParams>(Bliss.SphereColliderInitParams,bool)
		// object Bliss.Entity.AddChild<object,Bliss.UIAwakeInfo>(Bliss.UIAwakeInfo,bool)
		// object Bliss.Entity.AddChild<object,int>(int,bool)
		// object Bliss.Entity.AddChild<object,object>(object,bool)
		// object Bliss.Entity.AddChild<object>(bool)
		// object Bliss.Entity.AddChildWithId<object,Bliss.BulletCreateInfo>(long,Bliss.BulletCreateInfo,bool)
		// object Bliss.Entity.AddChildWithId<object,object>(long,object,bool)
		// object Bliss.Entity.AddChildWithId<object>(long,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.CharacterMovement.KinematicMoverManagerSettings>(Bliss.CharacterMovement.KinematicMoverManagerSettings,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.GameplayRigidbodyInitParams>(Bliss.GameplayRigidbodyInitParams,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.NetRoleComponentInitParam>(Bliss.NetRoleComponentInitParam,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.NetworkClientParam>(Bliss.NetworkClientParam,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.NetworkServerParam>(Bliss.NetworkServerParam,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.PeriodicMotionComponent.InitParam>(Bliss.PeriodicMotionComponent.InitParam,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.SmoothTransformComponentInitParam>(Bliss.SmoothTransformComponentInitParam,bool)
		// object Bliss.Entity.AddComponent<object,Bliss.ViewModel.GameObjectViewComponentInitParam>(Bliss.ViewModel.GameObjectViewComponentInitParam,bool)
		// object Bliss.Entity.AddComponent<object,int>(int,bool)
		// object Bliss.Entity.AddComponent<object,object>(object,bool)
		// object Bliss.Entity.AddComponent<object>(bool)
		// object Bliss.Entity.GetChild<object>(long)
		// object Bliss.Entity.GetComponent<object>()
		// System.Void Bliss.Entity.RemoveComponent<object>()
		// object Bliss.EntityHelper.GetSceneComponent<object>(Bliss.Entity)
		// System.Void Bliss.EntitySystem.Awake<Bliss.BoxColliderInitParams>(Bliss.Entity,Bliss.BoxColliderInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.BulletCreateInfo>(Bliss.Entity,Bliss.BulletCreateInfo)
		// System.Void Bliss.EntitySystem.Awake<Bliss.CapsuleColliderInitParams>(Bliss.Entity,Bliss.CapsuleColliderInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.CharacterMovement.KinematicMoverManagerSettings>(Bliss.Entity,Bliss.CharacterMovement.KinematicMoverManagerSettings)
		// System.Void Bliss.EntitySystem.Awake<Bliss.GameplayHeightFieldColliderInitParams>(Bliss.Entity,Bliss.GameplayHeightFieldColliderInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.GameplayMeshColliderInitParams>(Bliss.Entity,Bliss.GameplayMeshColliderInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.GameplayRigidbodyInitParams>(Bliss.Entity,Bliss.GameplayRigidbodyInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.NetRoleComponentInitParam>(Bliss.Entity,Bliss.NetRoleComponentInitParam)
		// System.Void Bliss.EntitySystem.Awake<Bliss.NetworkClientParam>(Bliss.Entity,Bliss.NetworkClientParam)
		// System.Void Bliss.EntitySystem.Awake<Bliss.NetworkServerParam>(Bliss.Entity,Bliss.NetworkServerParam)
		// System.Void Bliss.EntitySystem.Awake<Bliss.PeriodicMotionComponent.InitParam>(Bliss.Entity,Bliss.PeriodicMotionComponent.InitParam)
		// System.Void Bliss.EntitySystem.Awake<Bliss.SmoothTransformComponentInitParam>(Bliss.Entity,Bliss.SmoothTransformComponentInitParam)
		// System.Void Bliss.EntitySystem.Awake<Bliss.SphereColliderInitParams>(Bliss.Entity,Bliss.SphereColliderInitParams)
		// System.Void Bliss.EntitySystem.Awake<Bliss.UIAwakeInfo>(Bliss.Entity,Bliss.UIAwakeInfo)
		// System.Void Bliss.EntitySystem.Awake<Bliss.ViewModel.GameObjectViewComponentInitParam>(Bliss.Entity,Bliss.ViewModel.GameObjectViewComponentInitParam)
		// System.Void Bliss.EntitySystem.Awake<int>(Bliss.Entity,int)
		// System.Void Bliss.EntitySystem.Awake<object>(Bliss.Entity,object)
		// System.Void Bliss.EntitySystem.CallEventSystem<object,object>(Bliss.Entity)
		// System.Void Bliss.EventSystem.Invoke<Bliss.AddForceAtPositionParams>(long,Bliss.AddForceAtPositionParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayBoxColliderAwakeParams>(long,Bliss.GameplayBoxColliderAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayBoxColliderDestroyParams>(long,Bliss.GameplayBoxColliderDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayCapsuleColliderAwakeParams>(long,Bliss.GameplayCapsuleColliderAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayCapsuleColliderDestroyParams>(long,Bliss.GameplayCapsuleColliderDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayHeightFieldColliderAwakeParams>(long,Bliss.GameplayHeightFieldColliderAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayHeightFieldDestroyParams>(long,Bliss.GameplayHeightFieldDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayMeshColliderAwakeParams>(long,Bliss.GameplayMeshColliderAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayMeshColliderDestroyParams>(long,Bliss.GameplayMeshColliderDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayRigidbodyAwakeParams>(long,Bliss.GameplayRigidbodyAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplayRigidbodyDestroyParams>(long,Bliss.GameplayRigidbodyDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplaySphereColliderAwakeParams>(long,Bliss.GameplaySphereColliderAwakeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.GameplaySphereColliderDestroyParams>(long,Bliss.GameplaySphereColliderDestroyParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.Invoke_OnReceiveMessage>(long,Bliss.Invoke_OnReceiveMessage)
		// System.Void Bliss.EventSystem.Invoke<Bliss.MovePositionParams>(long,Bliss.MovePositionParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.MoveRotationParams>(long,Bliss.MoveRotationParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.Physics.Invoke_PhysicalSceneAwake>(long,Bliss.Physics.Invoke_PhysicalSceneAwake)
		// System.Void Bliss.EventSystem.Invoke<Bliss.Physics.Invoke_PhysicalSceneDestroy>(long,Bliss.Physics.Invoke_PhysicalSceneDestroy)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetAngularVelocityParams>(long,Bliss.SetAngularVelocityParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetBoxColliderCenterParams>(long,Bliss.SetBoxColliderCenterParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetBoxColliderSizeParams>(long,Bliss.SetBoxColliderSizeParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetCapsuleColliderCenterParams>(long,Bliss.SetCapsuleColliderCenterParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetCapsuleColliderDirectionParams>(long,Bliss.SetCapsuleColliderDirectionParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetCapsuleColliderHeightParams>(long,Bliss.SetCapsuleColliderHeightParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetCapsuleColliderRadiusParams>(long,Bliss.SetCapsuleColliderRadiusParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetCenterOfMassParams>(long,Bliss.SetCenterOfMassParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetMassParams>(long,Bliss.SetMassParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetPositionParams>(long,Bliss.SetPositionParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetRotationParams>(long,Bliss.SetRotationParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetSphereColliderCenterParams>(long,Bliss.SetSphereColliderCenterParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetSphereColliderRadiusParams>(long,Bliss.SetSphereColliderRadiusParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.SetVelocityParams>(long,Bliss.SetVelocityParams)
		// System.Void Bliss.EventSystem.Invoke<Bliss.WakeUpParams>(long,Bliss.WakeUpParams)
		// int Bliss.EventSystem.Invoke<Bliss.Physics.Invoke_SpereCastNonAlloc,int>(long,Bliss.Physics.Invoke_SpereCastNonAlloc)
		// object Bliss.EventSystem.Invoke<Bliss.ConfigManager.LoadFile,object>(long,Bliss.ConfigManager.LoadFile)
		// System.Void Bliss.EventSystem.Push<object,Bliss.Event_AfterCreateSceneActor>(object,Bliss.Event_AfterCreateSceneActor)
		// System.Void Bliss.EventSystem.Push<object,Bliss.Event_LoginSuccess>(object,Bliss.Event_LoginSuccess)
		// System.Void Bliss.EventSystem.Push<object,Bliss.Event_OnAcceiptConnection>(object,Bliss.Event_OnAcceiptConnection)
		// System.Void Bliss.EventSystem.Push<object,Bliss.Event_SyncViewTransform>(object,Bliss.Event_SyncViewTransform)
		// System.Void Bliss.EventSystem.Push<object,Bliss.Event_TransformChange>(object,Bliss.Event_TransformChange)
		// System.Void Bliss.EventSystem.Push<object,Bliss.GameplayEvent_ShootBullet>(object,Bliss.GameplayEvent_ShootBullet)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.BootProcess.Event_BootProcessFinish>(object,Bliss.BootProcess.Event_BootProcessFinish)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_AfterCreateGameplayWorld>(object,Bliss.Event_AfterCreateGameplayWorld)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_AfterCreateLevelScene>(object,Bliss.Event_AfterCreateLevelScene)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_AfterCreateSceneActor>(object,Bliss.Event_AfterCreateSceneActor)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_EndChangeLevelScene>(object,Bliss.Event_EndChangeLevelScene)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_EndCreateLevelScene>(object,Bliss.Event_EndCreateLevelScene)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_InitProcessAppInitFinish>(object,Bliss.Event_InitProcessAppInitFinish)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_InitProcessEntry_Demo>(object,Bliss.Event_InitProcessEntry_Demo)
		// ET.ETTask Bliss.EventSystem.PushAsync<object,Bliss.Event_StartChangeLevelScene>(object,Bliss.Event_StartChangeLevelScene)
		// object Bliss.Pool.ObjectPool.Fetch<object>(bool)
		// object Bliss.SimpleResourcesComponent.LoadAsset<object>(string)
		// object Bliss.World.AddSingleton<object,object>(object)
		// object Bliss.World.AddSingleton<object>()
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.C2G_EnterLevel_Handler.<Run>d__0>(ET.ETTaskCompleted&,Bliss.C2G_EnterLevel_Handler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHander_InitProcessEntryDemo.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHander_InitProcessEntryDemo.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHander_StartChangeLevelScene_UI.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHander_StartChangeLevelScene_UI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandle_Level_AfterCreateSceneActor.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandle_Level_AfterCreateSceneActor.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandler_AfterCreateGameplayWorld.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandler_AfterCreateGameplayWorld.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandler_EndChangeLevelScene.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandler_EndChangeLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandler_EndCreateLevelScene.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandler_EndCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.EventHandler_Level_SyncViewTransform.<Run>d__0>(ET.ETTaskCompleted&,Bliss.EventHandler_Level_SyncViewTransform.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.Logic.Client.EventHandler_OnAcceiptConnection_TestServer.<Run>d__0>(ET.ETTaskCompleted&,Bliss.Logic.Client.EventHandler_OnAcceiptConnection_TestServer.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.MessageHabdler_TransformSimpleSyncMessage.<Run>d__0>(ET.ETTaskCompleted&,Bliss.MessageHabdler_TransformSimpleSyncMessage.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.MessageHandler_M2C_CreateMyUnit.<Run>d__0>(ET.ETTaskCompleted&,Bliss.MessageHandler_M2C_CreateMyUnit.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0>(ET.ETTaskCompleted&,Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.MessageHandler_PropertySync_LongMessage.<Run>d__0>(ET.ETTaskCompleted&,Bliss.MessageHandler_PropertySync_LongMessage.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.PingMessageHandler.<Run>d__0>(ET.ETTaskCompleted&,Bliss.PingMessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.PongMessageHandler.<Run>d__0>(ET.ETTaskCompleted&,Bliss.PongMessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.UILoadingEvent.<AfterCreate>d__1>(ET.ETTaskCompleted&,Bliss.UILoadingEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.UILoginEvent.<AfterCreate>d__1>(ET.ETTaskCompleted&,Bliss.UILoginEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.UIMapSelectEvent.<AfterCreate>d__1>(ET.ETTaskCompleted&,Bliss.UIMapSelectEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0>(ET.ETTaskCompleted&,Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0>(ET.ETTaskCompleted&,Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<ET.ETTaskCompleted,Bliss.ViewLogic.Tests.EventHandler_SyncViewTransform.<Run>d__0>(ET.ETTaskCompleted&,Bliss.ViewLogic.Tests.EventHandler_SyncViewTransform.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.CameraComponentSystems.<SetupPlayerVirtualCameraWaitTargetViewLoad>d__7>(object&,Bliss.CameraComponentSystems.<SetupPlayerVirtualCameraWaitTargetViewLoad>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.Entry.<StartAsync>d__1>(object&,Bliss.Entry.<StartAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.EventHander_InitProcessEntryDemo.<Run>d__0>(object&,Bliss.EventHander_InitProcessEntryDemo.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.EventHander_LoginSucess_UI.<Run>d__0>(object&,Bliss.EventHander_LoginSucess_UI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0>(object&,Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.EventHandler_EndCreateLevelScene.<Run>d__0>(object&,Bliss.EventHandler_EndCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.EventHandler_StartChangeLevelScene.<Run>d__0>(object&,Bliss.EventHandler_StartChangeLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.Logic.InovkeHandler_RootInit_Main_Client.<Handle>d__0>(object&,Bliss.Logic.InovkeHandler_RootInit_Main_Client.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.LoginHelper.<Login>d__0>(object&,Bliss.LoginHelper.<Login>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MapHelper.<ChangeMap>d__1>(object&,Bliss.MapHelper.<ChangeMap>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MapHelper.<EnterMap>d__0>(object&,Bliss.MapHelper.<EnterMap>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MessageHandler_M2C_ChangeMap.<Run>d__0>(object&,Bliss.MessageHandler_M2C_ChangeMap.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MessageHandler_M2C_CreateBullets.<Run>d__0>(object&,Bliss.MessageHandler_M2C_CreateBullets.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MessageHandler_M2C_CreateSceneActors.<Run>d__0>(object&,Bliss.MessageHandler_M2C_CreateSceneActors.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MessageHandler_M2C_CreateUnits.<Run>d__0>(object&,Bliss.MessageHandler_M2C_CreateUnits.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0>(object&,Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ObjectWaitSystems.<>c__DisplayClass3_0.<<Wait>g__WaitTimeout|0>d<object>>(object&,Bliss.ObjectWaitSystems.<>c__DisplayClass3_0.<<Wait>g__WaitTimeout|0>d<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.SessionMessageHandler.<HandleAsync>d__3<object,object>>(object&,Bliss.SessionMessageHandler.<HandleAsync>d__3<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.SessionMessageHandler.<HandleAsync>d__3<object>>(object&,Bliss.SessionMessageHandler.<HandleAsync>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.SessionSystem.<>c__DisplayClass8_0.<<Call>g__Timeout|1>d>(object&,Bliss.SessionSystem.<>c__DisplayClass8_0.<<Call>g__Timeout|1>d&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.UIMapSelectComponentSystems.<EnterMap>d__3>(object&,Bliss.UIMapSelectComponentSystems.<EnterMap>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.Debugger.TransformStatDebugComponentSystems.<InitRes>d__6>(object&,Bliss.ViewLogic.Debugger.TransformStatDebugComponentSystems.<InitRes>d__6&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0>(object&,Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.Event_InitProcessEntry_InitClientGlobal.<Run>d__0>(object&,Bliss.ViewLogic.Event_InitProcessEntry_InitClientGlobal.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.GameObjectViewComponentSystems.<LoadViewmodel>d__4>(object&,Bliss.ViewLogic.GameObjectViewComponentSystems.<LoadViewmodel>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0>(object&,Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<object,Bliss.CameraComponentSystems.<WaitTargetViewLoaded>d__8>(object&,Bliss.CameraComponentSystems.<WaitTargetViewLoaded>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>>(System.Runtime.CompilerServices.TaskAwaiter&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>>(System.Runtime.CompilerServices.TaskAwaiter&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7>(System.Runtime.CompilerServices.TaskAwaiter&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.AUGUI_UIEvent.<OnCreate>d__1>(object&,Bliss.AUGUI_UIEvent.<OnCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ActorCreateHelper.<CreateTestActor>d__0>(object&,Bliss.ActorCreateHelper.<CreateTestActor>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.LevelSceneFactory.<Create>d__0>(object&,Bliss.LevelSceneFactory.<Create>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ObjectWaitSystems.<Wait>d__2<object>>(object&,Bliss.ObjectWaitSystems.<Wait>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ObjectWaitSystems.<Wait>d__3<object>>(object&,Bliss.ObjectWaitSystems.<Wait>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.SessionSystem.<Call>d__7>(object&,Bliss.SessionSystem.<Call>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.SessionSystem.<Call>d__8>(object&,Bliss.SessionSystem.<Call>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.UIComponentSystem.<Create>d__2>(object&,Bliss.UIComponentSystem.<Create>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.UIGlobalComponentSystem.<OnCreate>d__1>(object&,Bliss.UIGlobalComponentSystem.<OnCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>>(object&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>>(object&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<object,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7>(object&,Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.C2G_EnterLevel_Handler.<Run>d__0>(Bliss.C2G_EnterLevel_Handler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.CameraComponentSystems.<SetupPlayerVirtualCameraWaitTargetViewLoad>d__7>(Bliss.CameraComponentSystems.<SetupPlayerVirtualCameraWaitTargetViewLoad>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.Entry.<StartAsync>d__1>(Bliss.Entry.<StartAsync>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHander_InitProcessEntryDemo.<Run>d__0>(Bliss.EventHander_InitProcessEntryDemo.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHander_LoginSucess_UI.<Run>d__0>(Bliss.EventHander_LoginSucess_UI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHander_StartChangeLevelScene_UI.<Run>d__0>(Bliss.EventHander_StartChangeLevelScene_UI.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandle_Level_AfterCreateSceneActor.<Run>d__0>(Bliss.EventHandle_Level_AfterCreateSceneActor.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_AfterCreateGameplayWorld.<Run>d__0>(Bliss.EventHandler_AfterCreateGameplayWorld.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0>(Bliss.EventHandler_AfterCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_EndChangeLevelScene.<Run>d__0>(Bliss.EventHandler_EndChangeLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_EndCreateLevelScene.<Run>d__0>(Bliss.EventHandler_EndCreateLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_Level_SyncViewTransform.<Run>d__0>(Bliss.EventHandler_Level_SyncViewTransform.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventHandler_StartChangeLevelScene.<Run>d__0>(Bliss.EventHandler_StartChangeLevelScene.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.BootProcess.Event_BootProcessFinish>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.BootProcess.Event_BootProcessFinish>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateGameplayWorld>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateGameplayWorld>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateLevelScene>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateSceneActor>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_AfterCreateSceneActor>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_EndChangeLevelScene>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_EndChangeLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_EndCreateLevelScene>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_EndCreateLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_InitProcessAppInitFinish>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_InitProcessAppInitFinish>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_InitProcessEntry_Demo>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_InitProcessEntry_Demo>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_StartChangeLevelScene>>(Bliss.EventSystem.<PushAsync>d__1<object,Bliss.Event_StartChangeLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.Logic.Client.EventHandler_OnAcceiptConnection_TestServer.<Run>d__0>(Bliss.Logic.Client.EventHandler_OnAcceiptConnection_TestServer.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.Logic.InovkeHandler_RootInit_Main_Client.<Handle>d__0>(Bliss.Logic.InovkeHandler_RootInit_Main_Client.<Handle>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.LoginHelper.<Login>d__0>(Bliss.LoginHelper.<Login>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MapHelper.<ChangeMap>d__1>(Bliss.MapHelper.<ChangeMap>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MapHelper.<EnterMap>d__0>(Bliss.MapHelper.<EnterMap>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHabdler_TransformSimpleSyncMessage.<Run>d__0>(Bliss.MessageHabdler_TransformSimpleSyncMessage.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_ChangeMap.<Run>d__0>(Bliss.MessageHandler_M2C_ChangeMap.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_CreateBullets.<Run>d__0>(Bliss.MessageHandler_M2C_CreateBullets.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_CreateMyUnit.<Run>d__0>(Bliss.MessageHandler_M2C_CreateMyUnit.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_CreateSceneActors.<Run>d__0>(Bliss.MessageHandler_M2C_CreateSceneActors.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_CreateUnits.<Run>d__0>(Bliss.MessageHandler_M2C_CreateUnits.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0>(Bliss.MessageHandler_M2C_DestroyEntites.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.MessageHandler_PropertySync_LongMessage.<Run>d__0>(Bliss.MessageHandler_PropertySync_LongMessage.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ObjectWaitSystems.<>c__DisplayClass3_0.<<Wait>g__WaitTimeout|0>d<object>>(Bliss.ObjectWaitSystems.<>c__DisplayClass3_0.<<Wait>g__WaitTimeout|0>d<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.PingMessageHandler.<Run>d__0>(Bliss.PingMessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.PongMessageHandler.<Run>d__0>(Bliss.PongMessageHandler.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.SessionMessageHandler.<HandleAsync>d__3<object,object>>(Bliss.SessionMessageHandler.<HandleAsync>d__3<object,object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.SessionMessageHandler.<HandleAsync>d__3<object>>(Bliss.SessionMessageHandler.<HandleAsync>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.SessionSystem.<>c__DisplayClass8_0.<<Call>g__Timeout|1>d>(Bliss.SessionSystem.<>c__DisplayClass8_0.<<Call>g__Timeout|1>d&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.UILoadingEvent.<AfterCreate>d__1>(Bliss.UILoadingEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.UILoginEvent.<AfterCreate>d__1>(Bliss.UILoginEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.UIMapSelectComponentSystems.<EnterMap>d__3>(Bliss.UIMapSelectComponentSystems.<EnterMap>d__3&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.UIMapSelectEvent.<AfterCreate>d__1>(Bliss.UIMapSelectEvent.<AfterCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.Debugger.TransformStatDebugComponentSystems.<InitRes>d__6>(Bliss.ViewLogic.Debugger.TransformStatDebugComponentSystems.<InitRes>d__6&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0>(Bliss.ViewLogic.Event_InitProcessAppInitFinish_InitClientView.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.Event_InitProcessEntry_InitClientGlobal.<Run>d__0>(Bliss.ViewLogic.Event_InitProcessEntry_InitClientGlobal.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.GameObjectViewComponentSystems.<LoadViewmodel>d__4>(Bliss.ViewLogic.GameObjectViewComponentSystems.<LoadViewmodel>d__4&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0>(Bliss.ViewLogic.Tests.EventHandle_AfterCreateSceneActor_Client.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder.Start<Bliss.ViewLogic.Tests.EventHandler_SyncViewTransform.<Run>d__0>(Bliss.ViewLogic.Tests.EventHandler_SyncViewTransform.<Run>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<Bliss.Wait_AfterCreateGameObjectView>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_AfterCreateGameObjectView>>(Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_AfterCreateGameObjectView>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<Bliss.Wait_CreateLevelScene>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_CreateLevelScene>>(Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_CreateLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<Bliss.Wait_CreateMyUnit>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_CreateMyUnit>>(Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_CreateMyUnit>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<Bliss.Wait_FinishChangeLevelScene>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_FinishChangeLevelScene>>(Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_FinishChangeLevelScene>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<Bliss.Wait_LoadLevelSceneView>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_LoadLevelSceneView>>(Bliss.ObjectWaitSystems.<Wait>d__2<Bliss.Wait_LoadLevelSceneView>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<byte>.Start<Bliss.CameraComponentSystems.<WaitTargetViewLoaded>d__8>(Bliss.CameraComponentSystems.<WaitTargetViewLoaded>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.AUGUI_UIEvent.<OnCreate>d__1>(Bliss.AUGUI_UIEvent.<OnCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ActorCreateHelper.<CreateTestActor>d__0>(Bliss.ActorCreateHelper.<CreateTestActor>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.LevelSceneFactory.<Create>d__0>(Bliss.LevelSceneFactory.<Create>d__0&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ObjectWaitSystems.<Wait>d__2<object>>(Bliss.ObjectWaitSystems.<Wait>d__2<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ObjectWaitSystems.<Wait>d__3<object>>(Bliss.ObjectWaitSystems.<Wait>d__3<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.SessionSystem.<Call>d__7>(Bliss.SessionSystem.<Call>d__7&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.SessionSystem.<Call>d__8>(Bliss.SessionSystem.<Call>d__8&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.UIComponentSystem.<Create>d__2>(Bliss.UIComponentSystem.<Create>d__2&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.UIGlobalComponentSystem.<OnCreate>d__1>(Bliss.UIGlobalComponentSystem.<OnCreate>d__1&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>>(Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAllAssetsAsync>d__6<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>>(Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadAssetAsync>d__5<object>&)
		// System.Void ET.ETAsyncTaskMethodBuilder<object>.Start<Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7>(Bliss.ViewLogic.ResourcesLoaderComponentSystems.<LoadSceneAsync>d__7&)
		// string Luban.StringUtil.CollectionToString<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.List<object> MemoryPack.Formatters.ListFormatter.DeserializePackable<object>(MemoryPack.MemoryPackReader&)
		// System.Void MemoryPack.Formatters.ListFormatter.DeserializePackable<object>(MemoryPack.MemoryPackReader&,System.Collections.Generic.List<object>&)
		// System.Void MemoryPack.Formatters.ListFormatter.SerializePackable<object>(MemoryPack.MemoryPackWriter&,System.Collections.Generic.List<object>&)
		// byte[] MemoryPack.Internal.MemoryMarshalEx.AllocateUninitializedArray<byte>(int,bool)
		// byte& MemoryPack.Internal.MemoryMarshalEx.GetArrayDataReference<byte>(byte[])
		// MemoryPack.MemoryPackFormatter<object> MemoryPack.MemoryPackFormatterProvider.GetFormatter<object>()
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>()
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<int>()
		// bool MemoryPack.MemoryPackFormatterProvider.IsRegistered<object>()
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>(MemoryPack.MemoryPackFormatter<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>)
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<int>(MemoryPack.MemoryPackFormatter<int>)
		// System.Void MemoryPack.MemoryPackFormatterProvider.Register<object>(MemoryPack.MemoryPackFormatter<object>)
		// System.Void MemoryPack.MemoryPackReader.DangerousReadUnmanagedArray<byte>(byte[]&)
		// byte[] MemoryPack.MemoryPackReader.DangerousReadUnmanagedArray<byte>()
		// MemoryPack.IMemoryPackFormatter<object> MemoryPack.MemoryPackReader.GetFormatter<object>()
		// System.Void MemoryPack.MemoryPackReader.ReadPackable<object>(object&)
		// object MemoryPack.MemoryPackReader.ReadPackable<object>()
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>(Bliss.Gameplay.GameplayAttribute.PrimitiveValue&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Bliss.Mathematics.transform>(Bliss.Mathematics.transform&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float3,Unity.Mathematics.float3,long>(Unity.Mathematics.float3&,Unity.Mathematics.float3&,long&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float3,Unity.Mathematics.quaternion,Unity.Mathematics.float3,Unity.Mathematics.float3,byte,float>(Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,Unity.Mathematics.float3&,Unity.Mathematics.float3&,byte&,float&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float3,Unity.Mathematics.quaternion,float,float,int>(Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,float&,float&,int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float3,Unity.Mathematics.quaternion>(Unity.Mathematics.float3&,Unity.Mathematics.quaternion&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float3>(Unity.Mathematics.float3&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.float4>(Unity.Mathematics.float4&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<Unity.Mathematics.quaternion>(Unity.Mathematics.quaternion&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<byte>(byte&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<float>(float&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<int,int>(int&,int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<int,long>(int&,long&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<int>(int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,Bliss.Mathematics.transform,Bliss.Mathematics.transform>(long&,Bliss.Mathematics.transform&,Bliss.Mathematics.transform&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,Unity.Mathematics.float3,Unity.Mathematics.float4,Unity.Mathematics.float3,Unity.Mathematics.float3>(long&,Unity.Mathematics.float3&,Unity.Mathematics.float4&,Unity.Mathematics.float3&,Unity.Mathematics.float3&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,byte,int,long>(long&,byte&,int&,long&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,int,Unity.Mathematics.float3,Unity.Mathematics.quaternion,int,int>(long&,int&,Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,int&,int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,int,int>(long&,int&,int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,int,long>(long&,int&,long&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long,int>(long&,int&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanaged<long>(long&)
		// System.Void MemoryPack.MemoryPackReader.ReadUnmanagedArray<byte>(byte[]&)
		// byte[] MemoryPack.MemoryPackReader.ReadUnmanagedArray<byte>()
		// System.Void MemoryPack.MemoryPackReader.ReadValue<object>(object&)
		// object MemoryPack.MemoryPackReader.ReadValue<object>()
		// System.Void MemoryPack.MemoryPackWriter.DangerousWriteUnmanagedArray<byte>(byte[])
		// MemoryPack.IMemoryPackFormatter<object> MemoryPack.MemoryPackWriter.GetFormatter<object>()
		// System.Void MemoryPack.MemoryPackWriter.WritePackable<object>(object&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanaged<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>(Bliss.Gameplay.GameplayAttribute.PrimitiveValue&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanaged<long,int>(long&,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanaged<long>(long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedArray<byte>(byte[])
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<Unity.Mathematics.float3,Unity.Mathematics.float3,long>(byte,Unity.Mathematics.float3&,Unity.Mathematics.float3&,long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<Unity.Mathematics.float3,Unity.Mathematics.quaternion,Unity.Mathematics.float3,Unity.Mathematics.float3,byte,float>(byte,Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,Unity.Mathematics.float3&,Unity.Mathematics.float3&,byte&,float&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<Unity.Mathematics.float3,Unity.Mathematics.quaternion,float,float,int>(byte,Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,float&,float&,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<Unity.Mathematics.float3,Unity.Mathematics.quaternion>(byte,Unity.Mathematics.float3&,Unity.Mathematics.quaternion&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<int,int>(byte,int&,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<int,long>(byte,int&,long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<int>(byte,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,Bliss.Mathematics.transform,Bliss.Mathematics.transform>(byte,long&,Bliss.Mathematics.transform&,Bliss.Mathematics.transform&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,Unity.Mathematics.float3,Unity.Mathematics.float4,Unity.Mathematics.float3,Unity.Mathematics.float3>(byte,long&,Unity.Mathematics.float3&,Unity.Mathematics.float4&,Unity.Mathematics.float3&,Unity.Mathematics.float3&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,byte,int,long>(byte,long&,byte&,int&,long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,int,Unity.Mathematics.float3,Unity.Mathematics.quaternion,int,int>(byte,long&,int&,Unity.Mathematics.float3&,Unity.Mathematics.quaternion&,int&,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,int,int>(byte,long&,int&,int&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long,int,long>(byte,long&,int&,long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteUnmanagedWithObjectHeader<long>(byte,long&)
		// System.Void MemoryPack.MemoryPackWriter.WriteValue<object>(object&)
		// object System.Activator.CreateInstance<object>()
		// byte[] System.Array.Empty<byte>()
		// object[] System.Array.Empty<object>()
		// System.Collections.ObjectModel.ReadOnlyCollection<object> System.Dynamic.Utils.CollectionExtensions.ToReadOnly<object>(System.Collections.Generic.IEnumerable<object>)
		// int System.HashCode.Combine<object,byte>(object,byte)
		// object[] System.Linq.Enumerable.ToArray<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,System.Linq.Expressions.ParameterExpression[])
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,bool,System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>)
		// System.Linq.Expressions.Expression<object> System.Linq.Expressions.Expression.Lambda<object>(System.Linq.Expressions.Expression,string,bool,System.Collections.Generic.IEnumerable<System.Linq.Expressions.ParameterExpression>)
		// System.Span<byte> System.MemoryExtensions.AsSpan<byte>(byte[])
		// byte& System.Runtime.CompilerServices.Unsafe.Add<byte>(byte&,int)
		// byte& System.Runtime.CompilerServices.Unsafe.As<byte,byte>(byte&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.AsRef<object>(object&)
		// Bliss.Gameplay.GameplayAttribute.PrimitiveValue System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>(byte&)
		// Bliss.Mathematics.transform System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Bliss.Mathematics.transform>(byte&)
		// Unity.Mathematics.float3 System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Unity.Mathematics.float3>(byte&)
		// Unity.Mathematics.float4 System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Unity.Mathematics.float4>(byte&)
		// Unity.Mathematics.quaternion System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Unity.Mathematics.quaternion>(byte&)
		// byte System.Runtime.CompilerServices.Unsafe.ReadUnaligned<byte>(byte&)
		// float System.Runtime.CompilerServices.Unsafe.ReadUnaligned<float>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>(byte&)
		// long System.Runtime.CompilerServices.Unsafe.ReadUnaligned<long>(byte&)
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.Mathematics.transform>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Unity.Mathematics.float3>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Unity.Mathematics.float4>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<Unity.Mathematics.quaternion>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<byte>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<float>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<int>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<long>()
		// int System.Runtime.CompilerServices.Unsafe.SizeOf<object>()
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Bliss.Gameplay.GameplayAttribute.PrimitiveValue>(byte&,Bliss.Gameplay.GameplayAttribute.PrimitiveValue)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Bliss.Mathematics.transform>(byte&,Bliss.Mathematics.transform)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Unity.Mathematics.float3>(byte&,Unity.Mathematics.float3)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Unity.Mathematics.float4>(byte&,Unity.Mathematics.float4)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<Unity.Mathematics.quaternion>(byte&,Unity.Mathematics.quaternion)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<byte>(byte&,byte)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<float>(byte&,float)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<int>(byte&,int)
		// System.Void System.Runtime.CompilerServices.Unsafe.WriteUnaligned<long>(byte&,long)
		// byte& System.Runtime.InteropServices.MemoryMarshal.GetReference<byte>(System.Span<byte>)
		// object Unity.Cinemachine.CinemachineVirtualCamera.GetCinemachineComponent<object>()
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.Component.GetComponent<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// System.Void UnityEngine.GameObject.GetComponents<object>(System.Collections.Generic.List<object>)
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.CallbackContext.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ApplyProcessors<UnityEngine.Vector2>(int,UnityEngine.Vector2,UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// object UnityEngine.Object.Instantiate<object>(object)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// YooAsset.AllAssetsHandle YooAsset.ResourcePackage.LoadAllAssetsAsync<object>(string,uint)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetSync<object>(string)
		// YooAsset.AssetHandle YooAsset.YooAssets.LoadAssetSync<object>(string)
		// string string.Join<object>(string,System.Collections.Generic.IEnumerable<object>)
		// string string.JoinCore<object>(System.Char*,int,System.Collections.Generic.IEnumerable<object>)
	}
}