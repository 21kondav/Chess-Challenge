using ChessChallenge.API;
using System.Collections.Generic;
using System;
public class MyBot : IChessBot
{
    //todo: dyanmic bit boards
    //TODO: hotspot grid for pieves given a bit board set up
    private static decimal[] piece_vals = {1, 3, 3.2m, 5, 9}; //follows the same structure as PieceType enum
    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        //to remove in final 
        //set up necessary vars
        Move[] moves = board.GetLegalMoves();


        MoveEval no_move = new MoveEval(board);
        MoveEval alpha = new MoveEval(board, true);
        MoveEval beta = new MoveEval(board, false);
        MoveTree move_tree = new MoveTree(no_move, 4);
        move_tree.addChildren(move_tree);

        

        return move_tree.Minimax(alpha, beta).move;

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

        public MoveEval Minimax(MoveEval alpha, MoveEval beta ){
            //if we hit bottom of tree or no more legal moves

            if(this.max_height == 0 || this.data.board.GetLegalMoves().Length == 0)
                return this.data;
            
            MoveEval extreme = new MoveEval(this.data.board, this.data.colorToMove);
            foreach(MoveTree tree in this.children){
                MoveEval eval = tree.Minimax(alpha, beta);
                extreme = extreme.Compare(tree.data);
                if(this.data.colorToMove)
                    alpha = alpha.Compare(eval);
                else
                    beta = beta.Compare(eval);
                    
                if(beta.eval <= alpha.eval) break;
                
            }
            return extreme;
            
        }   

    }
    //general move eval for either side
    public struct MoveEval{
        public Move move;
        public Board board; //stores the board AFTER move
        public decimal eval;
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
            decimal pieceEval = 0;
            decimal board_eval = 0;
            //pieces evaluation
            if(b.IsInCheckmate()) board_eval += -decimal.MaxValue;
            board_eval += b.IsInCheck() ? -5 : 0;
            PieceList[] onBoard = board.GetAllPieceLists();
            
            foreach(PieceList p_list in onBoard){
                Console.WriteLine((int)p_list.TypeOfPieceInList);
                pieceEval += p_list.TypeOfPieceInList != PieceType.King? (p_list.IsWhitePieceList
                ? piece_vals[(int)p_list.TypeOfPieceInList- 1]: 
                    -piece_vals[(int)p_list.TypeOfPieceInList-1]):0;
                
                //if threat of caputre, reduce evaluation

            }
            //add piece eval to total 
            eval += pieceEval+board_eval;
            //generally we avoid check and mate
            b.UndoMove(m);
        }

        public MoveEval(Board b, bool? isMax= null){
            board = b;
            colorToMove = b.IsWhiteToMove;
            move = Move.NullMove;
            depth = 0;
            eval = isMax == null ? (colorToMove ?  -Decimal.MaxValue : Decimal.MaxValue): 
                    ((bool)isMax ? -Decimal.MaxValue : Decimal.MaxValue);
            
        }
        public MoveEval Compare(MoveEval otherMove){
            if(this.colorToMove){
                if(otherMove.eval > this.eval) return otherMove;
                return this;
            }
            if(otherMove.eval < this.eval) return otherMove;
            return this;
        }
        
    }
        //weighting pieces valuability
    //shouldn't matter piece color
    // public static decimal piece_eval(Piece p, Board board){
    //     int home_rank = board.GetKingSquare(board.IsWhiteToMove).Rank; 
    //     double b = 0; //shift up and down
    //     double a = .5; //amplitude
    //     double sigma = 2; //spread
    //     double xshift = 0; //shift along x axis
    //     double yshift = 0; //shift allong y
    //     double p_eval = 0;
    //     //roughly 40 moves per chess game
    //     int game_phase;
    //     sigma = Math.Log(board.PlyCount+6); //limit of this is 4.5 with in the chess limits
    //     if(board.PlyCount < 15) game_phase = 1;
    //     else if(board.PlyCount < 30) game_phase = 2;
    //     else game_phase = 3;
    //     switch(p.PieceType){
    //         case PieceType.Pawn:
    //             yshift += Math.Abs(3 - home_rank)*.1;
    //             if(game_phase == 3) yshift = Math.Abs(9 - home_rank);
    //             xshift += 4.5;
    //             p_eval +=  1;
    //             break;
    //         case PieceType.Knight:
    //             //approx opening game                
    //             yshift += 4.5;
    //             xshift += 4.5;
    //             if(game_phase == 1) yshift = Math.Abs( 4.5-home_rank)*.1;

    //             b = -.05;
    //             p_eval +=3 ;
    //             break;
    //         case PieceType.Bishop:
    //             p_eval += 3.2;
    //             break;
    //         case PieceType.Rook:
    //             p_eval += 5;
    //             break;
    //         case PieceType.Queen:
    //             p_eval += 9;
    //             break;
    //         case PieceType.King:

    //             break;
    //     }
        
    //     double exp = (Math.Pow(p.Square.File-xshift, 2) + Math.Pow(p.Square.Rank-yshift, 2))/Math.Pow(sigma, 2);
    //     p_eval += a*Math.Exp(-.5*exp) + b;
    //     return (decimal)p_eval;
    // }

}