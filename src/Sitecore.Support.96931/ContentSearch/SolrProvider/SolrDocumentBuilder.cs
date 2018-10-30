namespace Sitecore.Support.ContentSearch.SolrProvider
{
    using System;
    using System.Collections.Concurrent;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.Data.LanguageFallback;

    public class SolrDocumentBuilder : Sitecore.ContentSearch.SolrProvider.SolrDocumentBuilder
    {
        public SolrDocumentBuilder(IIndexable indexable, IProviderUpdateContext context) : base(indexable, context)
        {
        }

        protected override void AddComputedIndexFieldsInParallel()
        {
            ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();

            //ensure that we preserve current item-level language fallback setting when entering new threads
            var needEnterLanguageFallbackItemSwitcher = LanguageFallbackItemSwitcher.CurrentValue;

            this.ParallelForeachProxy.ForEach(
                this.Options.ComputedIndexFields,
                this.ParallelOptions,
                (computedIndexField, parallelLoopState) =>
                {
                    object fieldValue;

                    try
                    {
                        using (new LanguageFallbackItemSwitcher(needEnterLanguageFallbackItemSwitcher))
                        {
                            //take field-level language fallback setting into accout (fix for issue #96931)
                            using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
                            {
                                fieldValue = computedIndexField.ComputeFieldValue(this.Indexable);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CrawlingLog.Log.Warn(
                            string.Format("Could not compute value for ComputedIndexField: {0} for indexable: {1}",
                                computedIndexField.FieldName, this.Indexable.UniqueId), ex);
                        if (this.Settings.StopOnCrawlFieldError())
                        {
                            exceptions.Enqueue(ex);
                            parallelLoopState.Stop();
                        }

                        System.Diagnostics.Debug.WriteLine(ex);
                        return;
                    }

                    this.AddComputedIndexField(computedIndexField, fieldValue);
                });

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(exceptions);
            }
        }

        protected override void AddComputedIndexFieldsInSequence()
        {
            foreach (var computedIndexField in this.Options.ComputedIndexFields)
            {
                object fieldValue;

                try
                {
                    //take field-level language fallback setting into accout (fix for issue #96931)
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
                        throw;
                    }

                    System.Diagnostics.Debug.WriteLine(ex);
                    continue;
                }

                this.AddComputedIndexField(computedIndexField, fieldValue);
            }
        }
    }
}