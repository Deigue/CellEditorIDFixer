using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;

namespace CellEditorIDFixer
{
    public class CellEditorIDFixer
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences()
                {
                    AddImplicitMasters = false,
                    //IncludeDisabledMods = true,
                    ActionsForEmptyArgs = new RunDefaultPatcher
                    {
                        
                        IdentifyingModKey = "CellEditorIDFixer.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                }
            );
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Console.WriteLine($"Running Cell Editor ID Fixer ...");
            
            var counter = 0;

            foreach (var cellContext in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides())
            {
                //bool worldSpaceCellFlag = false;
                var cell = cellContext.Record;
                if ((cell.EditorID?.Contains("_") ?? false))
                {
                    Console.WriteLine($"Cell EDID {cell.EditorID} in {cell?.FormKey.ModKey.FileName}");

                    /*
                    foreach (var groupItem in state.LoadOrder.PriorityOrder.Worldspace().WinningOverrides())
                    {
                        if ( groupItem.EnumerateMajorRecords<ICellGetter>().Contains(cell))
                        {
                            Console.WriteLine($"ALERT: Worldspace CELL currently unsupported, SKIPPED: {cell.EditorID}");
                            worldSpaceCellFlag = true;
                            break;
                        }
                    }
                    
                    if (worldSpaceCellFlag) continue;
                    */

                    var overridenCell = cellContext.GetOrAddAsOverride(state.PatchMod);
                    overridenCell.EditorID = overridenCell.EditorID.Replace("_", "");
                    counter++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Made overrides for {counter} CELL records.");
        }
    }
}
