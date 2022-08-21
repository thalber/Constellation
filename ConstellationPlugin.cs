using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using MonoMod.RuntimeDetour;
using UnityEngine;

using static UnityEngine.Random;

namespace Constellation
{
    [BepInPlugin("thalber.Constellation", "Constellation", "0.1")]
    internal class ConstellationPlugin : BaseUnityPlugin
    {
        public void OnEnable()
        {
            try
            {
                On.Menu.EndgameMeter.NotchMeter.ctor += notches_ctor;
                On.Menu.EndgameMeter.NotchMeter.GrafUpdate += notches_gu;
            }
            catch (Exception ex) { Logger.LogWarning(ex); }
        }
        private bool started = false;

        private void notches_gu(On.Menu.EndgameMeter.NotchMeter.orig_GrafUpdate orig, Menu.EndgameMeter.NotchMeter self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.owner.tracker.ID is not WinState.EndgameID.Traveller) return;
            var meshreal = Mesh;

            for (int i = 0; i < self.notchSprites.GetLength(0); i++)
            {
                var npos = notchPosForMeter(self, i);
                var nbase = baseNotchPos(self, i);
                for (int j = 0; j < 4; j++)
                {
                    self.notchSprites[i, j].SetPosition(npos);
                }
                if (meshreal is null) continue;
                meshreal.vertices[i * 2] = npos;
                meshreal.vertices[i * 2 + 1] = Vector2.Lerp(npos, nbase, 0.5f);
            }
            if (meshreal is null) return;
            meshreal.vertices[meshreal.vertices.Length - 1] = self.meterTip;
            meshreal.vertices[0] = self.meterStart;
            for (int i = 0; i < meshreal.vertices.Length; i++) if (meshreal.vertices[i] == default) meshreal.vertices[i] = self.meterTip;
            if (!started)
            {
                Logger.LogError("started!"); started = true;
            }
        }

        private Vector2 baseNotchPos(Menu.EndgameMeter.NotchMeter inst, int notch) => Vector2.Lerp(inst.meterStart, inst.meterTip, (float)notch / inst.notchSprites.GetLength(0));
        private Vector2 notchPosForMeter(Menu.EndgameMeter.NotchMeter inst, int notch)
        {
            Vector2 basePos = baseNotchPos(inst, notch),
                meterDir = inst.meterTip - inst.meterStart;
            float maxdev = Mathf.Min(Vector2.Distance(inst.meterStart, basePos), Vector2.Distance(inst.meterTip, basePos)) / 3f;
            return basePos + RWCustom.Custom.PerpendicularVector(meterDir).normalized * offsets[notch] * maxdev;
        }


        private void notches_ctor(On.Menu.EndgameMeter.NotchMeter.orig_ctor orig, Menu.EndgameMeter.NotchMeter self, Menu.EndgameMeter owner)
        {
            orig(self, owner);

            offsets = new float[self.notchSprites.GetLength(0)];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = i % 2 == 0 ? 1f : -1f;
            var tmes = TriangleMesh.MakeLongMesh(offsets.Length / 2 + 1, true, true);
            tmes.element = Futile.atlasManager.GetElementWithName("Futile_White");
            for (int i = 0; i < tmes.verticeColors.Length; i++)
            {
                Color c = new();
                for (int u = 0; u < 3; u++) { c[u] = Range(0.8f, 1f); }
                c.a = 0.11f;
                tmes.verticeColors[i] = c;
            }
            __mesh = new(tmes);
            self.notchSprites[0, 0].container.AddChild(tmes);
        }

        public float[] offsets;
        private WeakReference __mesh;
        public TriangleMesh Mesh => __mesh?.Target as TriangleMesh;
        
    }
    internal static class ext
    {
        internal static int ornexteven(this int i) => i % 2 == 0 ? i : i + 1;
    }
}
