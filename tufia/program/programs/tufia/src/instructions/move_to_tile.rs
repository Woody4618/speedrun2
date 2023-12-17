use std::borrow::BorrowMut;

pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::player_data::PlayerData;
use anchor_lang::prelude::*;
use session_keys::{Session, SessionToken};

pub fn move_to_tile(mut ctx: Context<MoveToTile>, counter: u16, x: u64, y: u64) -> Result<()> {
    let account: &mut &mut MoveToTile<'_> = &mut ctx.accounts;
    account.player.update_energy()?;
    account.player.print()?;

    let authority = account.player.authority.key();

    if account.player.energy < 1 {
        return err!(GameErrorCode::NotEnoughEnergy);
    }

    account.player.last_id = counter;
    account.player.move_to_tile(x, y, authority)?;

    let game_data = &mut account.game_data.load_mut()?;

    game_data.move_to_tile(x, y, &mut account.player, true)?;

    Ok(())
}

#[derive(Accounts, Session)]
#[instruction(level_seed: String)]
pub struct MoveToTile<'info> {
    #[session(
        // The ephemeral key pair signing the transaction
        signer = signer,
        // The authority of the user account which must have created the session
        authority = player.authority.key()
    )]
    // Session Tokens are passed as optional accounts
    pub session_token: Option<Account<'info, SessionToken>>,

    // There is one PlayerData account
    #[account(
        mut,
        seeds = [b"player1".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,

    // There can be multiple levels the seed for the level is passed in the instruction
    // First player starting a new level will pay for the account in the current setup
    #[account(
        init_if_needed,
        payer = signer,
        space = 10240,
        seeds = [level_seed.as_ref()],
        bump,
    )]
    pub game_data: AccountLoader<'info, GameData>,

    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,
}
