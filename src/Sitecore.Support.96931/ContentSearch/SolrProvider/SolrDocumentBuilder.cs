using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.Data.LanguageFallback;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace Sitecore.Support.ContentSearch.SolrProvider
{
  public class SolrDocumentBuilder : Sitecore.ContentSearch.SolrProvider.SolrDocumentBuilder
  {
    private static readonly MethodInfo AddComputedIndexFieldMethodInfo;
    static SolrDocumentBuilder()
    {
      AddComputedIndexFieldMethodInfo = typeof(Sitecore.ContentSearch.SolrProvider.SolrDocumentBuilder).GetMethod("AddComputedIndexField", BindingFlags.Instance | BindingFlags.NonPublic);
    }
    public SolrDocumentBuilder(IIndexable indexable, IProviderUpdateContext context) : base(indexable, context)
    {
    }
    private void AddComputedIndexField(IComputedIndexField computedIndexField, ParallelLoopState parallelLoopState = null, ConcurrentQueue<Exception> exceptions = null)
    {
      AddComputedIndexFieldMethodInfo.Invoke(this, new object[]
      {
                computedIndexField, parallelLoopState, exceptions
      });
    }

    protected override void AddComputedIndexFieldsInParallel()
    {
      ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
      this.ParallelForeachProxy.ForEach<IComputedIndexField>((IEnumerable<IComputedIndexField>)base.Options.ComputedIndexFields, base.ParallelOptions,
        (Action<IComputedIndexField, ParallelLoopState>)((field, parallelLoopState) =>
        {
          using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
          {
            this.AddComputedIndexField(field, parallelLoopState, exceptions);
          }
        }));
      if (!exceptions.IsEmpty)
      {
        throw new AggregateException(exceptions);
      }
    }

    protected override void AddComputedIndexFieldsInSequence()
    {
      foreach (IComputedIndexField field in base.Options.ComputedIndexFields)
      {
        using (new LanguageFallbackFieldSwitcher(this.Index.EnableFieldLanguageFallback))
        {
          this.AddComputedIndexField(field, null, null);
        }
      }
    }

  }
}