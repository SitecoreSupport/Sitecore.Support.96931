namespace Sitecore.Support.ContentSearch.Azure
{
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.ComputedFields;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Data.LanguageFallback;
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Threading.Tasks;

  public class CloudSearchDocumentBuilder : Sitecore.ContentSearch.Azure.CloudSearchDocumentBuilder
  {
    public CloudSearchDocumentBuilder(IIndexable indexable, IProviderUpdateContext context)
        : base(indexable, context)
    {
    }
    protected override void AddComputedIndexFieldsInParallel()
    {
      ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
      var needEnterLanguageFallbackItemSwitcher = LanguageFallbackItemSwitcher.CurrentValue;
      ParallelForeachProxy.ForEach<IComputedIndexField>((IEnumerable<IComputedIndexField>)Options.ComputedIndexFields, ParallelOptions, (Action<IComputedIndexField, ParallelLoopState>)delegate (IComputedIndexField computedIndexField, ParallelLoopState parallelLoopState)
      {
        object fieldValue;
        try
        {
          using (new LanguageFallbackItemSwitcher(needEnterLanguageFallbackItemSwitcher))
          {
            using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
            {
              fieldValue = computedIndexField.ComputeFieldValue(Indexable);
            }
          }
        }
        catch (Exception ex)
        {
          CrawlingLog.Log.Warn($"Could not compute value for ComputedIndexField: {computedIndexField.FieldName} for indexable: {Indexable.UniqueId}", ex);
          if (Settings.StopOnCrawlFieldError())
          {
            exceptions.Enqueue(ex);
            parallelLoopState.Stop();
          }
          return;
        }
        using (new LanguageFallbackItemSwitcher(needEnterLanguageFallbackItemSwitcher))
        {
          using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
          {
            AddComputedIndexField(computedIndexField, fieldValue);
          }
        }
      });
      if (!exceptions.IsEmpty)
      {
        throw new AggregateException(exceptions);
      }
    }

    protected override void AddComputedIndexFieldsInSequence()
    {
      foreach (IComputedIndexField computedIndexField in Options.ComputedIndexFields)
      {
        object fieldValue;
        try
        {
          using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
          {
            fieldValue = computedIndexField.ComputeFieldValue(Indexable);
          }
        }
        catch (Exception exception)
        {
          CrawlingLog.Log.Warn($"Could not compute value for ComputedIndexField: {computedIndexField.FieldName} for indexable: {Indexable.UniqueId}", exception);
          if (Settings.StopOnCrawlFieldError())
          {
            throw;
          }
          continue;
        }
        using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
        {
          AddComputedIndexField(computedIndexField, fieldValue);
        }
      }
    }
  }
}