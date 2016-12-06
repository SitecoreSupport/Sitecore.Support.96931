namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading.Tasks;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.ComputedFields;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.Data.LanguageFallback;

    public class LuceneDocumentBuilder : Sitecore.ContentSearch.LuceneProvider.LuceneDocumentBuilder
    {
        private static readonly MethodInfo AddComputedIndexFieldMethodInfo;
        static LuceneDocumentBuilder()
        {
            AddComputedIndexFieldMethodInfo = typeof(Sitecore.ContentSearch.LuceneProvider.LuceneDocumentBuilder).GetMethod("AddComputedIndexField", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        public LuceneDocumentBuilder(IIndexable indexable, IProviderUpdateContext context) : base(indexable, context)
        {
        }

        protected override void AddComputedIndexFieldsInParallel()
        {
            ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
            Parallel.ForEach<IComputedIndexField>(base.Options.ComputedIndexFields, base.ParallelOptions, delegate (IComputedIndexField computedIndexField, ParallelLoopState parallelLoopState)
            {
                object fieldValue;
                try
                {
                    using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
                    {
                        fieldValue = computedIndexField.ComputeFieldValue(this.Indexable);
                    }                    
                }
                catch (Exception ex)
                {
                    CrawlingLog.Log.Warn(string.Format("Could not compute value for ComputedIndexField: {0} for indexable: {1}", computedIndexField.FieldName, this.Indexable.UniqueId), ex);
                    if (this.Settings.StopOnCrawlFieldError())
                    {
                        exceptions.Enqueue(ex);
                        parallelLoopState.Stop();
                    }
                    return;
                }
                this.AddComputedIndexField(computedIndexField, fieldValue);
            });
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private void AddComputedIndexField(IComputedIndexField computedIndexField, object fieldValue)
        {
            AddComputedIndexFieldMethodInfo.Invoke(this, new object[]
            {
                computedIndexField, fieldValue
            });
        }

        protected override void AddComputedIndexFieldsInSequence()
        {
            foreach (IComputedIndexField current in base.Options.ComputedIndexFields)
            {
                object fieldValue;
                try
                {
                    using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
                    {
                        fieldValue = current.ComputeFieldValue(base.Indexable);
                    }
                }
                catch (Exception exception)
                {
                    CrawlingLog.Log.Warn(string.Format("Could not compute value for ComputedIndexField: {0} for indexable: {1}", current.FieldName, base.Indexable.UniqueId), exception);
                    if (base.Settings.StopOnCrawlFieldError())
                    {
                        throw;
                    }
                    continue;
                }
                this.AddComputedIndexField(current, fieldValue);
            }
        }
    }
}