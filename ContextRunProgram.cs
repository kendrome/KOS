﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace kOS
{
    public class ContextRunProgram : ExecutionContext
    {
        private int accumulator;
        private File file;
        private String commandBuffer;
        private List<Command> commands = new List<Command>();
        private int executionLine = 0;

        public ContextRunProgram(ExecutionContext parent) : base(parent) 
        {
        }

        public void Run(File file)
        {
            this.file = file;

            State = ExecutionState.WAIT;

            int lineNumber = 0;
            foreach (String line in file)
            {
                commandBuffer += stripComment(line);

                string cmd;
                while (parseNext(ref commandBuffer, out cmd))
                {
                    try
                    {
                        Command cmdObj = Command.Get(cmd, this);
                        cmdObj.LineNumber = lineNumber;
                        commands.Add(cmdObj);
                    }
                    catch (kOSException e)
                    {
                        StdOut("Error on line " + lineNumber + ": " + e.Message);
                        State = ExecutionState.DONE;
                        return;
                    }
                }

                lineNumber++;
                accumulator++;
            }
        }

        public override bool Break()
        {
            State = ExecutionState.DONE;

            return true;
        }

        public string stripComment(string line)
        {
            for (var i=0; i<line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    i = Expression.FindEndOfString(line, i + 1);
                }
                else if (i < line.Length - 1 && line.Substring(i, 2) == "//")
                {
                    return line.Substring(0, i);
                }
            }

            return line;
        }
        
        public override void Update(float time)
        {
            base.Update(time);

            try
            {
                EvaluateNextCommand();
            }
            catch (kOSException e)
            {
                StdOut("Error on line " + executionLine + ": " + e.Message);
                State = ExecutionState.DONE;
                return;
            }
        }

        private void EvaluateNextCommand()
        {
            if (this.ChildContext == null)
            {
                if (commands.Count > 0)
                {
                    Command cmd = commands[0];
                    commands.RemoveAt(0);

                    ChildContext = cmd;
                    executionLine = cmd.LineNumber;
                    cmd.Evaluate();
                }
                else
                {
                    State = ExecutionState.DONE;
                }
            }
        }
    }
}
