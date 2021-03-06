﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AddressMatch
{
    public delegate bool MatchRule(State state, GraphNode node);

    public class MatchMachine
    {   
        private  MatchRule LocalMatchRule;

        private AddrSet _addrset;

        private bool defualtRule(State state, GraphNode nextNode)
        {
            if (nextNode.NodeLEVEL == LEVEL.Uncertainty || state.MinStateLEVEL == LEVEL.Uncertainty)
            {
                return true;
            }
            if (nextNode.NodeLEVEL > state.MinStateLEVEL)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public MatchMachine(AddrSet addrset)
        {
            if (addrset == null)
            {
                throw new Exception("AddrSet is not initialized");
            }
            if (AddrSet.AddrGraph == null)
            {
                throw new Exception("Graph is not initialized"); 
            }

            _addrset = addrset;

            init();
        }

        private void init()
        {
            LocalMatchRule = new MatchRule(defualtRule);
            //MatchStack = new Stack<State>();
        }

        
        /// <summary>
        /// Backward Match
        /// </summary>
        /// <param name="s">address string[]</param>
        /// <returns>result</returns>
        public MatchResult Match(String[] s)
        {
            MatchHelper.Assert(s.Count() == 0,
                                     @" input string[]'s length is 0 ");
            Stack<State> MatchStack = new Stack<State>();

            MatchResult result = new MatchResult();
            
            s.Reverse();
            //Store the first Match
            State firstState = new State();
            ReaderWriterLockSlim rwlock = _addrset.GetRWlock();
            rwlock.EnterReadLock();
            for (int i = 0; i < s.Count();i++)
            {
                State correntState = _addrset.FindNodeInHashTable(s[i]);
                if (i ==0)
                {
                    firstState = correntState;
                }
                if (correntState.NodeCount == 0)
                {
                    result.ResultState = MatchResultState.NOTFOUND;
                    goto RESULT;
                }
                //MatchStack.
                if (MatchStack.Count > 0)
                {
                    FilterState(correntState, MatchStack.Peek());
                    if (correntState.NodeCount == 0)
                    {
                        result.ResultState = MatchResultState.NOTMATCHED;
                        goto RESULT;
                    }
                }
                MatchStack.Push(correntState);
            }
            if (MatchStack.Count == 0)
            {
                result.ResultState = MatchResultState.NOTFOUND;
                goto RESULT;
            }
            if (MatchStack.Peek().NodeCount > 1)
            {
                result.ResultState = MatchResultState.MULTIMATCHED;
                goto RESULT;
            }


            List<GraphNode> resList;
            State TopState = MatchStack.Pop();
            do
            {
                State nextState = MatchStack.Pop();
                resList = 
                    _addrset.ForwardSearchNode(delegate(GraphNode node)
                {
                    return node.Name == nextState.Name;
                },
                TopState.NodeList);
                
                //if (resList.Count > 1)
                //{
                //    result.ResultState = MatchResultState.MULTIMATCHED;
                //    return result;
                //}
            } while (MatchStack.Count > 0);

            rwlock.ExitReadLock();

            if (resList == null || resList.Count == 0)
            {
                result.ResultState = MatchResultState.NOTMATCHED;
                goto RESULT;
            }

            if (resList.Count > 1)
            {
                result.ResultState = MatchResultState.MULTIMATCHED;
                goto RESULT;
            }

            result.Result = resList.First();
            result.ResultState = MatchResultState.SUCCESS;

        RESULT:
            rwlock.ExitReadLock();
            return result;

        }
        
        
        // TODO   Uncompleted
        public MatchResult FuzzyMatch()
        {

            return new MatchResult();
        }


        private State FilterState(State correntState,State preState)
        {
            correntState.NodeList.RemoveAll(delegate(GraphNode node)
            {
                return !LocalMatchRule(preState, node);
            });
            if (correntState.NodeList.Count() == 0)
            {
                correntState.MaxStateLEVEL = LEVEL.Uncertainty;
                correntState.MinStateLEVEL = LEVEL.Uncertainty;
                correntState.NodeCount = 0;
                correntState.NodeList = null;
            }
            else
            {
                //--------------------TODO   not effective
                LEVEL min = correntState.NodeList.First().NodeLEVEL;
                LEVEL max = correntState.NodeList.First().NodeLEVEL;

                foreach (GraphNode node in correntState.NodeList)
                {
                    min = min < node.NodeLEVEL ? min : node.NodeLEVEL;
                    max = max > node.NodeLEVEL ? max : node.NodeLEVEL;
                }

                correntState.MaxStateLEVEL = max;
                correntState.MinStateLEVEL = min;
                correntState.NodeCount = correntState.NodeList.Count();

            }
            return correntState;
        }
        

        #region -----------------------GET or SET-------------------------

        /// <summary>
        /// custom Match Rule
        /// </summary>
        /// <param name="rule">User-defined rule</param>
        public void SetMatchRule(MatchRule rule)
        {
            LocalMatchRule = rule;
        }


        #endregion

  
    }
}
