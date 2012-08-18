using System.Collections.Generic;
using System.Dynamic;

namespace HtmlTemplateEngine
{
    public class TemplateModelWrapper : DynamicObject
    {
        private readonly IDictionary<string, object> propertyMap;

        public TemplateModelWrapper(IDictionary<string, object> propertyMap)
        {
            Invariant.IsNotNull(propertyMap, "propertyMap");

            this.propertyMap = propertyMap;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return propertyMap.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            return binder != null && propertyMap.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder != null)
            {
                propertyMap[binder.Name] = value;

                return true;
            }

            return false;
        }
    }
}