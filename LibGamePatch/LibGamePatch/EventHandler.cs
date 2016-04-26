//
//  EventHandler.cs
//
//  Author:
//       Markus Kostrzewski <mkostrzewski@phoenix-dev.de>
//
//  Copyright (c) 2016 Phoenix Development
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace LibGamePatch
{
    public delegate void PatcherProgressChangedHandler(object source, PatcherProgressChangedArgs e);

    public delegate void PatcherStatusChangedHandler(object source, PatcherStatusChangedArgs e);

    public delegate void PatcherCompletedHandler(object source, PatcherCompletedArgs e);

    public class PatcherProgressChangedArgs : EventArgs
    {
        private int ProgressPercentage;

        public PatcherProgressChangedArgs(int percentage)
        {
            ProgressPercentage = percentage;
        }

        public int GetProgressPercentage()
        {
            return ProgressPercentage;
        }
    }

    public class PatcherStatusChangedArgs : EventArgs
    {
        private string Status;

        public PatcherStatusChangedArgs(string text)
        {
            Status = text;
        }

        public string GetStatus()
        {
            return Status;
        }
    }

    public class PatcherCompletedArgs : EventArgs
    {
        private int CompletedState;
        public PatcherCompletedArgs(int state)
        {
            CompletedState = state;
        }
        /// <summary>
        /// Returns the final state of the patching process.
        /// </summary>
        /// <returns>0 for patch successful and 1 for patch failed</returns>
        public int GetState()
        {
            return CompletedState;
        }
    }
}