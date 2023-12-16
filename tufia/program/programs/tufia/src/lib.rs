pub use crate::errors::GameErrorCode;
pub use anchor_lang::prelude::*;
pub use session_keys::{session_auth_or, Session, SessionError};
pub mod constants;
pub mod errors;
pub mod instructions;
pub mod state;
use instructions::*;

declare_id!("Bip92wN115UuArG265UHWZJVwuL64ymthirNZAw5jHYJ");

#[program]
pub mod tufia {

    use super::*;

    pub fn init_player(ctx: Context<InitPlayer>, _level_seed: String) -> Result<()> {
        init_player::init_player(ctx)
    }

    // This function moves the player to a new tile if he is on the board.
    // TODO: add enemies and chests
    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn move_to_tile(
        ctx: Context<MoveToTile>,
        _level_seed: String,
        counter: u16,
        x: u64,
        y: u64,
    ) -> Result<()> {
        move_to_tile::move_to_tile(ctx, counter, x, y)
    }

    // This function moves the player to a new tile if he is on the board.
    // TODO: add enemies and chests
    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn move_to_next_floor(
        ctx: Context<NextFloor>,
        _level_seed: String,
        counter: u16,
    ) -> Result<()> {
        next_floor::next_floor(ctx, counter)
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn buy_next_floor(
        ctx: Context<BuyNextFloor>,
        _level_seed: String,
        counter: u16,
    ) -> Result<()> {
        buy_next_floor::buy_next_floor(ctx, counter, _level_seed)
    }

    // This function moves the player to a new tile if he is on the board.
    // TODO: add enemies and chests
    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn reset_floor(ctx: Context<ResetFloor>, _level_seed: String, counter: u16) -> Result<()> {
        reset_floor::reset_floor(ctx, counter)
    }
}
