using ChessChallenge.API;
using System;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Linq;


public class EvilBot_1 : IChessBot
{
    DepthSearcher depthSearcher = new DepthSearcher();
    public static class Evaluator
    {
        public static float CountPiecesValueBalance(Board board)
        {
            float sum = 0f;
            foreach (PieceType piece in Enum.GetValues(typeof(PieceType)))
            {
                sum += Utils.GetPieceValue(piece) * (Utils.CountBits(board.GetPieceBitboard(piece, true)) - Utils.CountBits(board.GetPieceBitboard(piece, false)));
            }
            return sum;
        }
        public static float PushOpponentKingToTheEdge(Board board)
        {
            Square kingSquare = board.GetKingSquare(!board.IsWhiteToMove);
            Square oppKingSquare = board.GetKingSquare(board.IsWhiteToMove);

            int Hdist = Math.Min(3 - kingSquare.Rank, kingSquare.Rank - 4);
            int Wdist = Math.Min(3 - kingSquare.File, kingSquare.File - 4);

            int distBetweenKings = Math.Abs(oppKingSquare.Rank - kingSquare.Rank) + Math.Abs(oppKingSquare.File - kingSquare.File);

            return Hdist + Wdist + (14 - distBetweenKings);
        }
    }

    public static class Utils
    {
        public static float GetPieceValue(PieceType piece)
        {
            float[] pieceValues = { 0f, 1f, 3f, 3f, 3f, 9f, 0f };
            return pieceValues[(int)piece];
        }
        public static int CountBits(ulong value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }
        public static void Quicksort(System.Span<Move> values, int[] scores, int low, int high)
        {
            if (low < high)
            {
                int pivotIndex = Partition(values, scores, low, high);
                Quicksort(values, scores, low, pivotIndex - 1);
                Quicksort(values, scores, pivotIndex + 1, high);
            }
        }

        static int Partition(System.Span<Move> values, int[] scores, int low, int high)
        {
            int pivotScore = scores[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (scores[j] > pivotScore)
                {
                    i++;
                    (values[i], values[j]) = (values[j], values[i]);
                    (scores[i], scores[j]) = (scores[j], scores[i]);
                }
            }
            (values[i + 1], values[high]) = (values[high], values[i + 1]);
            (scores[i + 1], scores[high]) = (scores[high], scores[i + 1]);

            return i + 1;
        }
    }

    public static void OrderMoves(Move[] moves, Board board)
    {
        int[] moveScores = new int[moves.Count()];
        int i = 0;
        foreach (Move move in moves)
        {
            float moveScore = 0f;
            PieceType movedPieceType = move.MovePieceType;
            PieceType capturePieceType = move.CapturePieceType;

            if (capturePieceType != PieceType.None)
            {
                moveScore += 10f * Utils.GetPieceValue(capturePieceType) - Utils.GetPieceValue(movedPieceType);
            }

            if (move.IsPromotion)
            {
                moveScore += Utils.GetPieceValue(move.PromotionPieceType);
            }

            board.MakeMove(move);
            if (board.IsInCheck())
            {
                moveScore += 10f;
            }
            board.UndoMove(move);

            moveScores[i] = (int)moveScore;
            i += 1;
        }

        Utils.Quicksort(moves, moveScores, 0, i - 1);
    }

    public static float EvaluatePosition(Board board)
    {
        float mul = board.IsWhiteToMove ? 1f : -1f;

        float sum = 0f;

        sum += 1f * Evaluator.CountPiecesValueBalance(board);
        sum += 0.05f * Evaluator.PushOpponentKingToTheEdge(board);

        return sum * mul;
    }
    public class DepthSearcher
    {
        private Move bestMove;

        public Move GetMove(Board board)
        {
            return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove; // To avoid illegal move
        }
        public float DepthSearch(Board board, int depth, int maxDepth, float alpha, float beta, bool root = true)
        {
            if (depth == 0)
            {
                return SearchAllCaptures(board, alpha, beta);
            }

            if (root)
            {
                bestMove = Move.NullMove;
            }

            Move[] moves = board.GetLegalMoves();
            OrderMoves(moves, board);

            if (board.IsInCheckmate())
            {
                return float.NegativeInfinity;
            }

            if (board.IsDraw())
            {
                return 0f;
            }

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    if (depth == maxDepth)
                    {
                        bestMove = move;
                    }
                    board.UndoMove(move);
                    return float.PositiveInfinity;
                }
                float value = -DepthSearch(board, depth - 1, maxDepth, -beta, -alpha, false);
                board.UndoMove(move);

                if (value >= beta)
                {
                    return beta;
                }
                if (value > alpha)
                {
                    alpha = value;
                    if (root)
                    {
                        bestMove = move;
                    }
                }
            }
            return alpha;
        }

        public float SearchAllCaptures(Board board, float alpha, float beta)
        {

            if (board.IsInCheckmate())
            {
                return float.NegativeInfinity;
            }

            if (board.IsDraw())
            {
                return 0f;
            }

            float evaluation = EvaluatePosition(board);
            if (evaluation > beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);

            Move[] captureMoves = board.GetLegalMoves(capturesOnly: true);
            OrderMoves(captureMoves, board);

            foreach (Move move in captureMoves)
            {
                if (Utils.GetPieceValue(move.CapturePieceType) < Utils.GetPieceValue(move.MovePieceType))
                {
                    continue;
                }
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return float.PositiveInfinity;
                }
                evaluation = -SearchAllCaptures(board, -beta, -alpha);
                board.UndoMove(move);

                if (evaluation > beta)
                {
                    return beta;
                }

                alpha = Math.Max(alpha, evaluation);
            }

            return alpha;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        depthSearcher.DepthSearch(board, 4, 4, float.NegativeInfinity, float.PositiveInfinity, true);
        return depthSearcher.GetMove(board);
    }
}