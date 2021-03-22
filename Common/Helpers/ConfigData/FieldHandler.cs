using ImJustMatt.Common.Integrations.GenericModConfigMenu;
using StardewModdingAPI;

namespace Helpers.ConfigData
{
    internal class FieldHandler : BaseFieldHandler
    {
        private readonly IFieldHandler _fieldHandler;

        public FieldHandler(IFieldHandler fieldHandler)
        {
            _fieldHandler = fieldHandler;
        }

        public override object GetValue(object instance, IField field)
        {
            if (_fieldHandler?.CanHandle(field) ?? false)
            {
                return _fieldHandler.GetValue(instance, field);
            }

            return base.GetValue(instance, field);
        }

        public override void SetValue(object instance, IField field, object value)
        {
            if (_fieldHandler?.CanHandle(field) ?? false)
            {
                _fieldHandler.SetValue(instance, field, value);
            }
            else
            {
                base.SetValue(instance, field, value);
            }
        }

        public override void CopyValue(IField field, object source, object target)
        {
            if (_fieldHandler?.CanHandle(field) ?? false)
            {
                _fieldHandler.CopyValue(field, source, target);
            }
            else
            {
                base.CopyValue(field, source, target);
            }
        }

        public override void RegisterConfigOption(IManifest manifest, GenericModConfigMenuIntegration modConfigMenu, object instance, IField field)
        {
            if (_fieldHandler?.CanHandle(field) ?? false)
            {
                _fieldHandler.RegisterConfigOption(manifest, modConfigMenu, instance, field);
            }
            else
            {
                base.RegisterConfigOption(manifest, modConfigMenu, instance, field);
            }
        }
    }
}