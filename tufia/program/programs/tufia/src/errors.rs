use anchor_lang::error_code;

#[error_code]
pub enum GameErrorCode {
    #[msg("Not enough energy")]
    NotEnoughEnergy,
    #[msg("Wrong Authority")]
    WrongAuthority,
    #[msg("Player not on board")]
    PlayerNotOnBoard,
    #[msg("Out of bounds")]
    OutOfBounds,

    #[msg("PlayerAlreadyExists")]
    PlayerAlreadyExists,

    #[msg("BoardIsFull")]
    BoardIsFull,

    #[msg("PlayerIsAlreadyOnThisTile")]
    PlayerIsAlreadyOnThisTile,
}
