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
                    //AddImplicitMasters = false,
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

            int counter = 0;
            int worldCounter = 0;
            var worldContexts = state.LoadOrder.PriorityOrder.Worldspace().WinningContextOverrides();
            var cellWorldspaceMap = new Dictionary<FormKey, ModContext<ISkyrimMod, IWorldspace, IWorldspaceGetter>>();

            state.LoadOrder.PriorityOrder
                .ForEach(mod =>
                    {
                        mod.Mod?.Worldspaces.RecordCache.Items
                            .ForEach(world =>
                                {
                                    var context = worldContexts
                                        .Where(wc => wc.Record.FormKey.ID == world.FormKey.ID)
                                        .First();
                                    /*
                                    context = new ModContext<ISkyrimMod, IWorldspace, IWorldspaceGetter>(
                                            modKey: mod.ModKey,
                                            record: world,
                                            getter: (m, r) => m.Worldspaces.GetOrAddAsOverride(r)
                                    );
                                    */
                                    world.EnumerateMajorRecords<ICellGetter>()
                                        .Select(c => c.FormKey)
                                        .ForEach(cellKey => cellWorldspaceMap.TryAdd(cellKey, context));
                                }
                            );
                    }
                );


            // ATTEMPT 1: Attempt to create HashMap of desired values <CELL FormKey, Worldspace>...
            // Tuple.Create(w, w.EnumerateMajorRecords<ICellGetter>()))
            //   .SelectMany(t => t.Item2)
            // .Select(rec => Tuple.Create(rec.FormKey, w))
            /*
            Select(x => Tuple.Create(x, x.Item2.)
            SelectMany(x => x.EnumerateMajorRecords<ICellGetter>())

            .Select(x =>
            {
                FormKey = x.FormKey;
                IWorldspaceGetter = x.p
            })
            .ToDictionary<FormKey, IWorldspaceGetter>();
            */

            /* ATTEMPT 2: WorldspaceContext map to cells. Cannot do this, CELLS inside Winning Worldspace Context
             * are completely different from ACTUALLY winning cells. Contains() check will fail aftewords as it wont find the necessary CELLs.
            var worldSpaces = state.LoadOrder.PriorityOrder.Worldspace().WinningContextOverrides();
            worldSpaces.ForEach(w =>
                {
                    
                    w.Record.EnumerateMajorRecords<ICellGetter>()
                        .Select(c => c.FormKey)
                        .ForEach(f =>
                        {
                            cellWorldspaceMap.Add(f, w);
                        });
                   
                });
            */

            // ATTEMPT 3: Try direct map with worldspace search. Not possible since difficult to obtain worldspace from CellContext.
            //<Worldspace FormKey, Worldspace|WorldspaceContext>
            /*
            var worldSpaces = state.LoadOrder.PriorityOrder.Worldspace().WinningContextOverrides();
            worldSpaces.ForEach(w =>
                {
                    cellWorldspaceMap.Add(w.Record.FormKey, w);
                });
            */

            /* Summary:
             * Looped search from worldspace -> group -> subgroup cell wont work as it will give the cells contained in the winning worldspaceContext,
             * which does not cover/contain all the winning worldspace cells, since some actually reside in losing worldspace contexts.
             */

            foreach (var cellContext in state.LoadOrder.PriorityOrder.Cell().WinningContextOverrides())
            {
                bool worldSpaceCellFlag = false;
                
                var cell = cellContext.Record;
                if ((cell.EditorID?.Contains("_") ?? false))
                {
                    Console.WriteLine($"Cell EDID {cell.EditorID} in {cell.FormKey.ModKey.FileName}");

                    if (cell.Grid != null)
                    {
                        //Console.WriteLine($">>> SKIPPED {cell.EditorID} as Worldspace CELL is currently unsupported.");
                        worldSpaceCellFlag = true;
                    }

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
                    */

                    
                    if (worldSpaceCellFlag)
                    {


                        /*
                        if (worldSpaceFormIds.Contains(overridenCell.FormKey))
                        {
                            worldSpaceFormIds.
                        }*/

                        var worldSpaceCell = cellContext.Record;

                        if (cellWorldspaceMap.ContainsKey(worldSpaceCell.FormKey))
                        {
                            cellWorldspaceMap.TryGetValue(worldSpaceCell.FormKey, out var worldContext);
                            worldContext.GetOrAddAsOverride(state.PatchMod);
                            Console.WriteLine($"Overwrote the worldspace {worldContext.Record.Name} - {worldContext.Record.FormKey.ID} from {worldContext.ModKey.FileName} into the patch");
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
