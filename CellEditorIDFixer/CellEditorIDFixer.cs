using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

            int counter = 0;
            int worldCounter = 0;
            var worldContexts = state.LoadOrder.PriorityOrder.Worldspace().WinningContextOverrides();
            var cellWorldspaceMap = new Dictionary<FormKey, ModContext<ISkyrimMod, IWorldspace, IWorldspaceGetter>>();

            // Build Map to provide path to the winning worldspace context.
            state.LoadOrder.PriorityOrder
                .ForEach(mod =>
                    {
                        mod.Mod?.Worldspaces.RecordCache.Items
                            .ForEach(world =>
                                {
                                    var winningContext = worldContexts
                                        .Where(wc => wc.Record.FormKey.Equals(world.FormKey))
                                        .First();

                                    world.EnumerateMajorRecords<ICellGetter>()
                                        .Select(c => c.FormKey)
                                        .ForEach(cellKey => cellWorldspaceMap.TryAdd(cellKey, winningContext));
                                }
                            );
                    }
                );

            foreach (var cellContext in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides())
            {
                bool worldSpaceCellFlag = false;
                
                var cell = cellContext.Record;
                if ((cell.EditorID?.Contains("_") ?? false))
                {
                    Console.WriteLine($"Cell EDID {cell.EditorID} in {cell.FormKey.ModKey.FileName}");

                    if (cell.Grid != null)
                    {
                        worldSpaceCellFlag = true;
                    }

                    if (worldSpaceCellFlag)
                    {

                        if (cellWorldspaceMap.ContainsKey(cell.FormKey))
                        {
                            cellWorldspaceMap.TryGetValue(cell.FormKey, out var worldContext);
                            worldContext.GetOrAddAsOverride(state.PatchMod);
                            //Console.WriteLine($"Overwrote the worldspace {worldContext.Record.Name} ID:{worldContext.Record.FormKey}" +
                            //    $" from {worldContext.ModKey.FileName} into the patch.");
                        }

                        var overridenCell = cellContext.GetOrAddAsOverride(state.PatchMod);
                        overridenCell.EditorID = overridenCell.EditorID?.Replace("_", "");
                        overridenCell.Persistent.Clear();
                        overridenCell.Temporary.Clear();
                        worldCounter++;
                    }
                    else
                    {
                        var overridenCell = cellContext.GetOrAddAsOverride(state.PatchMod);
                        overridenCell.EditorID = overridenCell.EditorID?.Replace("_", "");
                        counter++;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Made overrides for {counter} CELL records.");
            Console.WriteLine($"Made overrides for {worldCounter} Worldspace CELL records.");
        }
    }
}
