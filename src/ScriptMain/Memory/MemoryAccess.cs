﻿using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TornadoScript.ScriptMain.Memory
{
    public static unsafe class MemoryAccess
    {
        private static bool bInitialized = false;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate IntPtr FwGetAssetIndexFn(IntPtr assetStore, out int index, StringBuilder name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int AddEntityToPoolFn(ulong address);

        public delegate IntPtr GetPooledPtfxAddressFn(int handle);

        private static IntPtr PtfxAssetStorePtr;

        private static IntPtr ScriptEntityPoolAddr, VehiclePoolAddr, PedPoolAddr, ObjectPoolAddr;

        private static FwGetAssetIndexFn FwGetAssetIndex;

        public static AddEntityToPoolFn AddEntityToPool;

        private static readonly uint PtfxColourHash = (uint)Game.GenerateHash("ptxu_Colour");

        private static Dictionary<string, IntPtr> ptfxRulePtrList = new Dictionary<string, IntPtr>();

        /*
        struct fwPool
        {
         void *m_pData;
         unsigned __int8 *m_bitMap;
         int m_count;
         int m_itemSize;
         int unkItemIndex;
         int nextFreeSlotIndex;
         unsigned int m_flags;
         char pad1[4];
         };
         */

        public static void Initialize()
        {
            #region SetupPTFXAssetStore

            var pattern = new Pattern("\x0F\xBF\x04\x9F\xB9", "xxxxx");

            var result = pattern.Get(0x19);

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64() + 7;
                var value = Marshal.ReadInt32(IntPtr.Add(result, 3));
                PtfxAssetStorePtr = new IntPtr(rip + value);
            }

            #endregion

            #region SetupfwGetAssetIndex

            pattern = new Pattern("\x41\x8B\xDE\x4C\x63\x00", "xxxxx?");

            result = pattern.Get();

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64();
                var value = Marshal.ReadInt32(result - 4);
                FwGetAssetIndex = Marshal.GetDelegateForFunctionPointer<FwGetAssetIndexFn>(new IntPtr(rip + value));
            }

            // Entity Pool ->

            pattern = new Pattern("\x4C\x8B\x0D\x00\x00\x00\x00\x44\x8B\xC1\x49\x8B\x41\x08", "xxx????xxxxxxx");

            result = pattern.Get(7);

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64();
                var value = Marshal.ReadInt32(result - 4);
                ScriptEntityPoolAddr = Marshal.ReadIntPtr(new IntPtr(rip + value));

               // UI.ShowSubtitle(ScriptEntityPoolAddr.ToString("X"));
            }

            // Vehicle Pool ->

            pattern = new Pattern("\x48\x8B\x05\x00\x00\x00\x00\xF3\x0F\x59\xF6\x48\x8B\x08", "xxx????xxxxxxx");

            result = pattern.Get(7);

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64();
                var value = Marshal.ReadInt32(result - 4);
                VehiclePoolAddr = Marshal.ReadIntPtr(new IntPtr(rip + value));

             //   UI.ShowSubtitle(VehiclePoolAddr.ToString("X"));
            }

            // Ped Pool ->

            pattern = new Pattern("\x48\x8B\x05\x00\x00\x00\x00\x41\x0F\xBF\xC8\x0F\xBF\x40\x10", "xxx????xxxxxxxx");

            result = pattern.Get(7);

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64();
                var value = Marshal.ReadInt32(result - 4);
                PedPoolAddr = Marshal.ReadIntPtr(new IntPtr(rip + value));

              //  UI.ShowSubtitle(PedPoolAddr.ToString("X"));
            }

            // Object Pool ->

            pattern = new Pattern("\x48\x8B\x05\x00\x00\x00\x00\x8B\x78\x10\x85\xFF", "xxx????xxxxx");

            result = pattern.Get(7);

            if (result != IntPtr.Zero)
            {
                var rip = result.ToInt64();
                var value = Marshal.ReadInt32(result - 4);
                ObjectPoolAddr = Marshal.ReadIntPtr(new IntPtr(rip + value));

              //  UI.ShowSubtitle(ObjectPoolAddr.ToString("X"));
            }

            pattern = new Pattern("\x48\xF7\xF9\x49\x8B\x48\x08\x48\x63\xD0\xC1\xE0\x08\x0F\xB6\x1C\x11\x03\xD8", "xxxxxxxxxxxxxxxxxxx");

            result = pattern.Get();

            if (result != IntPtr.Zero)
            {
                AddEntityToPool = Marshal.GetDelegateForFunctionPointer<AddEntityToPoolFn>(IntPtr.Subtract(result, 0x68));

                //UI.ShowSubtitle(result.ToString("X"));
            }

            // WinHelper.CopyTlsValues(WinHelper.GetProcessMainThreadId(), Win32Native.GetCurrentThreadId(), 0xC8, 0xC0, 0xB8);

            #endregion

            bInitialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetEntityPosition(IntPtr entity)
        {
            return (Vector3)Marshal.PtrToStructure(entity + 0x90, typeof(Vector3));
        }

        public static unsafe IList<Entity> CollectEntitiesFull()
        {
            if (!bInitialized)
                return null;

            FwPool* entityPool = (FwPool*)ScriptEntityPoolAddr;
            VehiclePool* vehiclePool = *(VehiclePool**)VehiclePoolAddr;
            GenericPool* pedPool = (GenericPool*)PedPoolAddr;
            GenericPool* objectPool = (GenericPool*)ObjectPoolAddr;

            List<Entity> list = new List<Entity>();

            // Vehicles
            for (uint i = 0; i < vehiclePool->size; i++)
            {
                if (entityPool->IsFull())
                    break;

                if (vehiclePool->IsValid(i))
                {
                    var vehicle = Entity.FromHandle(AddEntityToPool(vehiclePool->poolAddress[i])) as Vehicle;
                    if (vehicle != null) list.Add(vehicle);
                }
            }

            // Peds
            for (uint i = 0; i < pedPool->size; i++)
            {
                if (entityPool->IsFull())
                    break;

                if (pedPool->IsValid(i))
                {
                    var address = pedPool->GetAddress(i);
                    if (address != 0)
                    {
                        var ped = Entity.FromHandle(AddEntityToPool(address)) as Ped;
                        if (ped != null) list.Add(ped);
                    }
                }
            }

            // Objects/Props
            for (uint i = 0; i < objectPool->size; i++)
            {
                if (entityPool->IsFull())
                    break;

                if (objectPool->IsValid(i))
                {
                    var address = objectPool->GetAddress(i);
                    if (address != 0)
                    {
                        var prop = Entity.FromHandle(AddEntityToPool(address)) as Prop;
                        if (prop != null) list.Add(prop);
                    }
                }
            }

            return list;
        }


        public static IEnumerable<Entity> GetAllEntitiesInternal()
        {
            if (!bInitialized)
                yield break;

            var poolItems = Marshal.ReadIntPtr(ScriptEntityPoolAddr);
            var bitMap = Marshal.ReadIntPtr(ScriptEntityPoolAddr + 0x8);
            var count = Marshal.ReadInt32(ScriptEntityPoolAddr + 0x10);

            for (int i = 0; i < count; i++)
            {
                var bitset = Marshal.ReadByte(bitMap + i);
                if ((bitset & 0x80) != 0)
                    continue;

                var handle = (i << 8) + bitset;
                var type = Function.Call<int>(Hash.GET_ENTITY_TYPE, handle);

                Entity entity = Entity.FromHandle(handle);
                if (entity == null)
                    continue;

                switch (type)
                {
                    case 1: // Ped
                        if (entity is Ped ped) yield return ped;
                        break;
                    case 2: // Vehicle
                        if (entity is Vehicle veh) yield return veh;
                        break;
                    case 3: // Prop
                        if (entity is Prop prop) yield return prop;
                        break;
                }
            }
        }


        private static PgDictionary* GetPtfxRuleDictionary(string ptxAssetName)
        {
            if (bInitialized == false)
                return null;

            var assetStore = Marshal.PtrToStructure<PtfxAssetStore>(PtfxAssetStorePtr);

            FwGetAssetIndex(PtfxAssetStorePtr, out var index, new StringBuilder(ptxAssetName));

            var ptxFxListPtr = Marshal.ReadIntPtr(assetStore.Items + assetStore.ItemSize * index);

            return (PgDictionary*)Marshal.ReadIntPtr(ptxFxListPtr + 0x48);
        }

        public static bool FindPtxEffectRule(PgDictionary* ptxRulesDict, string fxName, out IntPtr result)
        {
            if (bInitialized == false)
            {
                result = IntPtr.Zero;
                return false;
            }

            for (var i = 0; i < ptxRulesDict->ItemsCount; i++)
            {
                var itAddress = Marshal.ReadIntPtr(ptxRulesDict->Items + i * 8);

                if (itAddress == IntPtr.Zero) continue;

                var szName = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(itAddress + 0x20));

                if (szName != fxName) continue;

                result = itAddress;

                return true;
            }

            result = IntPtr.Zero;

            return false;
        }

        /// <summary>
        /// Get emitter by its name for the given asset rule
        /// </summary>
        /// <param name="ptxAssetRulePtr">Pointer to the PtfxAssetRule instance</param>
        /// <param name="emitterName">Name of the child emitter object</param>
        /// <returns></returns>
        private static PtxEventEmitter* GetPtfxEventEmitterByName(IntPtr ptxAssetRulePtr, string emitterName)
        {
            if (bInitialized == false)
                return null;

            PtxEventEmitter* foundEmitter = null;

            var ptxRule = Marshal.PtrToStructure<PtxEffectRule>(ptxAssetRulePtr);

            for (var i = 0; i < ptxRule.EmittersCount; i++)
            {
                var emitter = ptxRule.Emitters[i];

                var szName = Marshal.PtrToStringAnsi(emitter->SzEmitterName);

                if (szName == emitterName)
                {
                    foundEmitter = emitter;

                    break;
                }
            }

            return foundEmitter;
        }

        /// <summary>
        /// Lightweight function for when we know the emitters index
        /// </summary>
        /// <param name="ptxAssetRulePtr"></param>
        /// <param name="emitterIndex"></param>
        /// <returns></returns>
        private static PtxEventEmitter* GetPtfxEventEmitterByIndex(IntPtr ptxAssetRulePtr, int emitterIndex)
        {
            return (*(PtxEventEmitter***)IntPtr.Add(ptxAssetRulePtr, 0x38))[emitterIndex];
        }

        public static void SetPtfxLOD(string baseAsset, string particleName)
        {
            string key = baseAsset + ':' + particleName;

            if (!ptfxRulePtrList.TryGetValue(key, out var result) &&
                !FindPtxEffectRule(GetPtfxRuleDictionary(baseAsset), particleName, out result))
            {
                return;
            }

            ptfxRulePtrList[key] = result;
        }

        public static void SetPtfxColor(string baseAsset, string particleName, int emitterIndex, Color newColor)
        {
            if (bInitialized == false)
                return;

            string key = baseAsset + ':' + particleName;

            if (!ptfxRulePtrList.TryGetValue(key, out var result) && 
                !FindPtxEffectRule(GetPtfxRuleDictionary(baseAsset), particleName, out result)) {
                return;
            }

            ptfxRulePtrList[key] = result;

            PtxEventEmitter* emitter = GetPtfxEventEmitterByIndex(result, emitterIndex);

            Debug.Assert(emitter != null);

            SetEmitterColour(emitter, newColor);
        }

        private static void SetEmitterColour(PtxEventEmitter* emitter, Color colour)
        {
            SetEmitterColour(emitter, colour.R, colour.G, colour.B, colour.A);
        }

        private static void SetEmitterColour(PtxEventEmitter* emitter, byte red, byte green, byte blue, byte alpha)
        {      
            var r = 1.0f / 255 * red;
            var g = 1.0f / 255 * green;
            var b = 1.0f / 255 * blue;
            var a = 1.0f / 255 * alpha;

            for (var i = 0; i < emitter->ParticleRule->BehavioursCount; i++)
            {
                Ptxu_Colour* behaviour = emitter->ParticleRule->Behaviours[i];

                if (behaviour->HashName != PtfxColourHash) continue;

                for (var x = 0; x < behaviour->NumFrames; x++)
                {
                    PtxKeyframeProp* keyframe = behaviour->KeyframeProps[x];

                    if (keyframe->Current.Items == IntPtr.Zero) continue;

                    var items = (PtxVarVector*)keyframe->Current.Items;

                    for (var y = 0; y < keyframe->Current.Count; y++)
                    {
                        if (items == null) continue;

                        items[y].Min.R = r;
                        items[y].Min.G = g;
                        items[y].Min.B = b;
                        items[y].Min.A = a;

                        items[y].Max.R = r;
                        items[y].Max.G = g;
                        items[y].Max.B = b;
                        items[y].Max.A = a;
                    }
                }

                break;
            }
        }
    }
}
