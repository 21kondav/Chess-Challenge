using ChessChallenge.API;
using System.Collections.Generic;
using System;
public class MyBot : IChessBot
{
    //todo: dyanmic bit boards
    //TODO: hotspot grid for pieves given a bit board set up
    private static double[] piece_vals = {1,  3, 3.2, 5, 9}; //follows the same structure as PieceType enum
    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        //to remove in final 
        //set up necessary vars
        Move[] moves = board.GetLegalMoves();
        Random rand = new Random();
        int ply = board.PlyCount;

        MoveEval no_move = new MoveEval(board);
        MoveTree move_tree = new MoveTree(no_move, 4);
        move_tree.addChildren(move_tree);
        move_tree.GetPaths(move_tree, move_tree.AddToList);
        // move_tree.PurgeTree();
                //evaluate board piece
        LinkedList<MoveEval> best = move_tree.paths[0];
        double bestVal = MoveEval.PathEval(best);
        foreach(LinkedList<MoveEval> path in move_tree.paths){
            if(path.Count < 2) continue;
            double val =  MoveEval.PathEval(path);
            if(isWhite){
                if(val > bestVal){
                    best = path;
                    bestVal = val;
                }
            }else{
                if(val < bestVal){
                    best = path;
                    bestVal = val;
                }                
            }

        }
        return best.First.Next.Value.move;

    }
    //weighting pieces valuability
    public static double gaussian(int x, int y){
        double total = 0;
        double exp = (Math.Pow(x-4, 2) + Math.Pow(y-4, 2))*.5;
        double board_prob = .2*Math.Exp(-exp/2);
        total += board_prob;
        return total;
    }

    public static LinkedList<MoveEval> AddToList(MoveEval node, LinkedList<MoveEval> path){
        if (path == null) path = new LinkedList<MoveEval>();
        
        if (path.Count == 0 || path.Last.Value.depth == node.depth - 1)
        {        
            path.AddLast(node);//add to path
        }

        return path;
    }
    public delegate void DoSomething<MoveTree, LinkedList>(MoveTree node, LinkedList<MoveEval> path);
    public class MoveTree{
        public MoveEval data;
        public int max_height;
        public LinkedList<MoveTree> children;
        public List<LinkedList<MoveEval>> paths;

        public MoveTree(MoveEval aNode, int max_height){
            data = aNode;
            this.max_height = max_height;
            children = new LinkedList<MoveTree>();
            this.paths = new List<LinkedList<MoveEval>>();
        }
        public void addChildren(MoveTree node)
        {   
            if(node.max_height == 0) return;
            //defines with width
            foreach(Move m in node.data.board.GetLegalMoves()){
                MoveEval data = new MoveEval(m, node.data.board, node.data.depth + 1);
                bool add = true; //should add
              //pruning
              //if this is blacks move
                if(data.depth % 2 == 0){
                    //foreach kid already in children
                    foreach(MoveTree kid in node.children){
                        //if our current move is worse
                        if(data.eval < kid.data.eval){
                            add = false;
                            break;
                        }
                    }
                }
                if(add) node.children.AddFirst(new MoveTree(data, node.max_height - 1));
            }
            foreach(MoveTree kid in node.children){
              addChildren(kid);
            }
            
        }
        public void AddToList(MoveTree node, LinkedList<MoveEval> path)
        {
          while(path.Count > node.data.depth){
            path.RemoveLast();
          }
          if (path.Count == 0 || path.Last.Value.depth == node.data.depth - 1)
          {        
            path.AddLast(node.data);//add to path
            if(node.children.Count == 0){
              this.paths.Add(new LinkedList<MoveEval>(path));
            }
          }
        }
        public void GetPaths(MoveTree node, DoSomething<MoveTree, LinkedList<MoveEval>> visit, LinkedList<MoveEval> path = null)
        {
            if(path ==null) path = new LinkedList<MoveEval>();
            visit(node, path);
            foreach (MoveTree kid in node.children)
            {   
                GetPaths(kid, visit, path);
            }
        }
    }
    //general move eval for either side
    public struct MoveEval{
        public Move move;
        public Board board; //stores the board AFTER move
        public double eval;
        public bool colorToMove;
        public int depth;
        //takes a board before a move and the move to be made
        //scoring is relaive to the color
        public MoveEval(Move m, Board b, int depth = 0){
            move = m;
            colorToMove = b.IsWhiteToMove;
            b.MakeMove(move);
            board = b;
            eval = 0;
            this.depth = depth;
            //pieces evaluation
            PieceList[] onBoard = board.GetAllPieceLists();
            double pieceEval = 0;
            for(int i = 0; i < 11; i++){
                if(i == 5){
                    continue;
                }
                double extra_value = 0;
                if(onBoard[i].TypeOfPieceInList == PieceType.Pawn){
                    foreach(Piece p in onBoard[i]){
                        extra_value += MyBot.gaussian(p.Square.File, p.Square.Index);
                    }
                    
                }else if(onBoard[i].TypeOfPieceInList == PieceType.Knight){
                    foreach(Piece p in onBoard[i]){
                        extra_value += MyBot.gaussian(p.Square.File, p.Square.Rank);
                    } 
                }
                //if threat of caputre, reduce evaluation
                if(!onBoard[i].IsWhitePieceList){
                    pieceEval -= (extra_value + onBoard[i].Count*piece_vals[i-6]);
                }else{
                    pieceEval += extra_value + onBoard[i].Count*piece_vals[i];
                }
            }
        
            //add piece eval to total 
            eval += pieceEval;

            b.UndoMove(m);
        }
        public MoveEval(Board b){
            board = b;
            colorToMove = b.IsWhiteToMove;
            move = Move.NullMove;
            eval = 0;
            depth = 0;
        }
        public static double PathEval(LinkedList<MoveEval> path){

            double sum = 0;
            foreach(MoveEval move in path){
                sum += move.eval;
            }
            return sum;
        }
 
    }
}