using Chess.Domain.ValueObjects;

namespace Chess.Domain.Chess.Rules;

public sealed class ClassicRuleSet : IRuleSet
{
    public string VariantId => "Classic";

    public IReadOnlyList<Move> GetLegalMoves(BoardState board, PieceColor side) =>
        MoveGenerator.GetLegalMoves(board, side);

    public IReadOnlyList<PieceType> GetPromotionChoices() =>
        new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };

    public bool IsInCheck(BoardState board, PieceColor side) =>
        MoveGenerator.IsKingInCheck(board, side);

    public bool IsCheckmate(BoardState board) =>
        IsInCheck(board, board.CurrentTurn) &&
        !MoveGenerator.GetLegalMoves(board, board.CurrentTurn).Any();

    public bool IsStalemate(BoardState board) =>
        !IsInCheck(board, board.CurrentTurn) &&
        !MoveGenerator.GetLegalMoves(board, board.CurrentTurn).Any();

    public bool IsDrawByRules(BoardState board, IReadOnlyList<string> positionHistory)
    {
        if (DrawDetector.IsFiftyMoveRule(board.HalfmoveClock)) return true;
        if (DrawDetector.IsInsufficientMaterial(board)) return true;
        if (DrawDetector.IsThreefoldRepetition(positionHistory)) return true;
        return false;
    }

    public bool IsCastlingLegal(BoardState board, Square from, Square to)
    {
        var piece = board.GetPiece(from);
        if (piece is null || piece.Type != PieceType.King) return false;
        var moves = MoveGenerator.GetLegalMoves(board, piece.Color);
        return moves.Any(m => m.From == from && m.To == to && (m.IsCastleKingSide || m.IsCastleQueenSide));
    }

    public bool IsEnPassantLegal(BoardState board, Square from, Square to)
    {
        var moves = MoveGenerator.GetLegalMoves(board, board.CurrentTurn);
        return moves.Any(m => m.From == from && m.To == to && m.IsEnPassant);
    }

    public bool IsPromotionRequired(BoardState board, Square from, Square to) =>
        MoveGenerator.IsPromotionRequired(board, from, to);

    public MoveResult ValidateMove(BoardState board, Square from, Square to, PieceType? promotion = null)
    {
        if (!from.IsValid || !to.IsValid)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "خانه نامعتبر" };

        var piece = board.GetPiece(from);
        if (piece is null)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "خانه مبدأ خالی است" };

        if (piece.Color != board.CurrentTurn)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "نوبت شما نیست" };

        var target = board.GetPiece(to);
        if (target?.Color == piece.Color)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "نمی‌توان مهره خودی را گرفت" };

        var legalMoves = MoveGenerator.GetLegalMoves(board, board.CurrentTurn);
        var matchingMove = promotion.HasValue
            ? legalMoves.FirstOrDefault(m => m.From == from && m.To == to && m.PromotionPiece == promotion)
            : legalMoves.FirstOrDefault(m => m.From == from && m.To == to);

        if (matchingMove is null)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "الگوی حرکت مجاز نیست" };

        if (IsPromotionRequired(board, from, to) && promotion is null)
            return new MoveResult { Status = MoveResultStatus.Illegal, Reason = "ترفیع اجباری است" };

        var sim = MoveGenerator.SimulateMove(board, matchingMove);
        bool isCheck = IsInCheck(sim, board.CurrentTurn.Opposite());
        bool isMate = IsCheckmate(sim);
        bool isStale = IsStalemate(sim);

        string san = MoveGenerator.GenerateSan(board, matchingMove, isCheck, isMate);

        if (isMate)
            return new MoveResult { Status = MoveResultStatus.Checkmate, SanNotation = san };
        if (isCheck)
            return new MoveResult { Status = MoveResultStatus.Check, SanNotation = san };
        if (isStale)
            return new MoveResult { Status = MoveResultStatus.Stalemate, SanNotation = san };

        return new MoveResult { Status = MoveResultStatus.Legal, SanNotation = san };
    }
}
