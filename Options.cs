﻿/* Copyright (c) 2016 xanthalas.co.uk
 * 
 * Author: Xanthalas
 * Date  : June 2016
 * 
 *  This file is part of csql
 *
 *  TfsCli is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  TfsCli is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with csql.  If not, see <http://www.gnu.org/licenses/>.
 */
using CommandLine;

namespace csql
{
    public class Options
    {
        [Option('h', "help", Required = false, HelpText = "Show this help.")]
        public bool Help { get; set; }

        [Option('l', "list", DefaultValue = false, HelpText = "List available databases")]
        public bool ShowList { get; set; }

        [Option('w', "wide", DefaultValue = false, HelpText = "Show output in line-mode (wide)")]
        public bool Wide { get; set; }

        [Option('v', "verbose", DefaultValue = false, HelpText = "Show verbose output")]
        public bool Verbose { get; set; }

        [Option('s', "select", DefaultValue = -1, HelpText = "Select database")]
        public int SelectedDatabase { get; set; }

    }
}