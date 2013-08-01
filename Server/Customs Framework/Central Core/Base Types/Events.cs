using System;

namespace CustomsFramework
{
    public delegate void BaseCoreEventHandler(BaseCoreEventArgs e);
    public delegate void BaseModuleEventHandler(BaseModuleEventArgs e);

    public class BaseCoreEventArgs : EventArgs
    {
        private BaseCore m_Core;

        public BaseCoreEventArgs(BaseCore core)
        {
            m_Core = core;
        }

        public BaseCore Core
        {
            get
            {
                return this.m_Core;
            }
        }
    }

    public class BaseModuleEventArgs : EventArgs
    {
        private BaseModule m_Module;

        public BaseModuleEventArgs(BaseModule module)
        {
            m_Module = module;
        }

        public BaseModule Module
        {
            get
            {
                return this.m_Module;
            }
        }
    }
}