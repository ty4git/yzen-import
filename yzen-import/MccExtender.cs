using System;
using System.Linq;
using System.Threading.Tasks;
using YzenImport.AlfaBank;
using YzenImport.Helpers;

namespace YzenImport
{
    internal class MccExtender
    {
        private static readonly string MCC = "MCC";
        private readonly MccsDynamicCache _mccsCache;

        public MccExtender(MccsDynamicCache mccCache)
        {
            _mccsCache = mccCache;
        }

        public async Task<OperationsContent<ExtendedOperation>> Extend(OperationsContent<Operation> operations)
        {
            var ops = operations.GetOperations();

            var extendingTasks = ops
                .AsParallel()
                .AsOrdered()
                .Select(async op => await ExtendOperation(op))
                .ToArray();

            var exOps = await Task.WhenAll(extendingTasks);

            var newCnt = new OperationsContent<ExtendedOperation>(exOps);
            return newCnt;
        }

        private async Task<ExtendedOperation> ExtendOperation(Operation operation)
        {
            var mcc = ExtractMcc(operation.Description);
            if (mcc is null)
            {
                var invalidMcc = -1;
                var exop = new ExtendedOperation(operation)
                {
                    MCC = invalidMcc.ToString(),
                    Category = $"Warning: MCC not found (MCC: {invalidMcc})"
                };
                return exop;
            }

            var mccDesc = await _mccsCache.Get(mcc.Value);
            var category = $"{mccDesc} (MCC: {mcc})";

            return new ExtendedOperation(operation)
            {
                MCC = mcc?.ToString(),
                Category = category
            };
        }

        private int? ExtractMcc(string opDesc)
        {
            var mccs = opDesc.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.StartsWith(MCC, StringComparison.OrdinalIgnoreCase))
                .Select(possibleMcc => Converters.ToMcc(string.Concat(possibleMcc.Skip(MCC.Length))))
                .Where(mcc => mcc != null)
                .Select(mcc => mcc.Value)
                .ToArray();

            if (mccs.Length == 0)
            {
                return null;
            }

            if (mccs.Length > 1)
            {
                throw new Exception($"There are 2 or more MCC ({string.Join(", ", mccs)})"); //todo: print in out file or other file with errors
            }

            return mccs.Single();
        }
    }
}
