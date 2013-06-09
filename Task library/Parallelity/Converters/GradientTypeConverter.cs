using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Parallelity.Drawing;

namespace Parallelity.Converters
{
    public static class Gradients
    {
        public static List<Gradient> All
        {
            get
            {
                return typeof(Gradient)
                                    .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                    .Where(property => property.GetValue(null, null) is Gradient)
                                    .Select(property => (Gradient)property.GetValue(null, null))
                                    .ToList();
            }
        }
    }

    public class GradientTypeConverter : GenericTypeConverter<Gradient>
    {
        protected override List<Gradient> GetAll()
        {
            return Gradients.All;
        }

        protected override String GetName(Gradient instance)
        {
            return instance.Name;
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
