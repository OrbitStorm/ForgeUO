/***************************************************************************
*                                 Prompt.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: Prompt.cs 644 2010-12-23 09:18:45Z asayre $
*
***************************************************************************/








/***************************************************************************
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   (at your option) any later version.
*
***************************************************************************/
using System;

namespace Server.Prompts
{
    public abstract class Prompt
    {
        private static int m_Serials;
        private readonly int m_Serial;
        protected Prompt()
        {
            do
            {
                this.m_Serial = ++m_Serials;
            }
            while (this.m_Serial == 0);
        }

        public int Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        public virtual void OnCancel(Mobile from)
        {
        }

        public virtual void OnResponse(Mobile from, string text)
        {
        }
    }
}