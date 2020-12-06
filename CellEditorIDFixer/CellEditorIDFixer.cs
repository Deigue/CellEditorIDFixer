using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;

namespace CellEditorIDFixer
{
    public static class CellEditorIdFixer
    {
        
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher
                    {
                        
                        IdentifyingModKey = "CellEditorIDFixer.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                }
            );
        }

        private static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine($"Running Cell Editor ID Fixer ...");

            var counter = 0;

            foreach (var cellContext in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides(state.LinkCache))
            {
                var cell = cellContext.Record;
                if ((!(cell.EditorID?.Contains("_") ?? false))) continue;
                Console.WriteLine($"Cell EDID {cell.EditorID} in {cell.FormKey.ModKey.FileName}");
                var overridenCell = cellContext.GetOrAddAsOverride(state.PatchMod);
                overridenCell.EditorID = overridenCell.EditorID?.Replace("_", "");
                counter++;
            }

            Console.WriteLine();
            Console.WriteLine($"Made overrides for {counter} CELL records.");
        }
    }
}
