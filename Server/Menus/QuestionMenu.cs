/***************************************************************************
*                              QuestionMenu.cs
*                            -------------------
*   begin                : May 1, 2002
*   copyright            : (C) The RunUO Software Team
*   email                : info@runuo.com
*
*   $Id: QuestionMenu.cs 4 2006-06-15 04:28:39Z mark $
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
using Server.Network;

namespace Server.Menus.Questions
{
    public class QuestionMenu : IMenu
    {
        private static int m_NextSerial;
        private readonly string[] m_Answers;
        private readonly int m_Serial;
        private string m_Question;
        public QuestionMenu(string question, string[] answers)
        {
            this.m_Question = question;
            this.m_Answers = answers;

            do
            {
                this.m_Serial = ++m_NextSerial;
                this.m_Serial &= 0x7FFFFFFF;
            }
            while (this.m_Serial == 0);
        }

        public string Question
        {
            get
            {
                return this.m_Question;
            }
            set
            {
                this.m_Question = value;
            }
        }
        public string[] Answers
        {
            get
            {
                return this.m_Answers;
            }
        }
        int IMenu.Serial
        {
            get
            {
                return this.m_Serial;
            }
        }
        int IMenu.EntryLength
        {
            get
            {
                return this.m_Answers.Length;
            }
        }
        public virtual void OnCancel(NetState state)
        {
        }

        public virtual void OnResponse(NetState state, int index)
        {
        }

        public void SendTo(NetState state)
        {
            state.AddMenu(this);
            state.Send(new DisplayQuestionMenu(this));
        }
    }
}