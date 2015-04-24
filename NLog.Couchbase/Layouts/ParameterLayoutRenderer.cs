using NLog.Config;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NLog.LayoutRenderers
{
    [LayoutRenderer("parameter-value")]
    public class ParameterLayoutRenderer : LayoutRenderer
    {
        /// <summary>
        /// Gets or sets the name Parameter index to use and respective property navigation separated by dots.
        /// For example: parameter-value:item=0.PropName
        /// gets the value of the property 'PropName' of the first Parameter (0 index in the Parameters array).
        /// Sub property names are supported like 'PropName1.SubPropName2' altough it's very inneficient to use in this way.
        /// </summary>
        [RequiredParameter]
        [DefaultParameter]
        public string Item { get; set; }


        /// <summary>
        /// Renders the specified log event context item and appends it to the specified <see cref="StringBuilder" />.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the rendered data to.</param>
        /// <param name="logEvent">Logging event.</param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {         
            if (string.IsNullOrWhiteSpace(Item))
                return;

            var propNav = Item.Split('.');
            var parameterIndexStr = propNav.FirstOrDefault();
            var parameterIndex = int.Parse(propNav.FirstOrDefault());

            if (string.IsNullOrWhiteSpace(parameterIndexStr) || !int.TryParse(parameterIndexStr, out parameterIndex))
                return;

            var parameterValue = logEvent.Parameters[parameterIndex];
            object value = null;

            try
            {
                value = GetPropertyValue(parameterValue, string.Join(".", propNav.Skip(1)));
            }
            catch (Exception e)
            {
                if (LogManager.ThrowExceptions)
                    throw e;
                //ignore
            }

            if (value != null)
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        public object GetPropertyValue(object oRecord, string propertyNavigation)
        {
            if (string.IsNullOrWhiteSpace(propertyNavigation))
                return string.Empty;

            var propertyStructure = propertyNavigation.Split('.');

            System.Type oType = null;
            object value = null;
            oType = oRecord.GetType();

            PropertyInfo[] cProperties;

            cProperties = oType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < propertyStructure.Length; i++)
            {
                string propertyName = propertyStructure[i];
                foreach (PropertyInfo theProperty in cProperties)
                {
                    if (string.Equals(theProperty.Name, propertyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if ((i + 1) < propertyStructure.Length)
                        {
                            value = GetPropertyValue(theProperty.GetValue(oRecord, null), string.Join(".", propertyStructure.Skip(i + 1).ToArray()));
                        }
                        else
                        {
                            value = theProperty.GetValue(oRecord, null);
                        }

                        return value;
                    }
                }

            }
            return string.Empty;
        }
    }


}
