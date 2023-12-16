use std::char::CharTryFromError;

pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::{game_data::TileData, player_data::PlayerData};
use anchor_lang::prelude::{borsh::de, *};
use session_keys::{Session, SessionToken};

pub fn buy_next_floor(
    mut ctx: Context<BuyNextFloor>,
    counter: u16,
    _level_seed: String,
) -> Result<()> {
    let account: &mut &mut BuyNextFloor<'_> = &mut ctx.accounts;
    account.player.last_id = counter;

    let game_data = &mut account.game_data.load_init()?;
    game_data.owner = account.signer.key();

    let mut tile_data_clone: TileData = TileData::default();

    tile_data_clone.tile_armor = account.player.tile_data.tile_armor;
    tile_data_clone.tile_damage = account.player.tile_data.tile_damage;
    tile_data_clone.tile_defence = account.player.tile_data.tile_defence;
    tile_data_clone.tile_health = account.player.tile_data.tile_health;
    tile_data_clone.tile_level = account.player.tile_data.tile_level;
    tile_data_clone.tile_max_armor = account.player.tile_data.tile_max_armor;
    tile_data_clone.tile_max_health = account.player.tile_data.tile_max_health;
    tile_data_clone.tile_owner = account.player.tile_data.tile_owner;
    tile_data_clone.tile_type = account.player.tile_data.tile_type;
    tile_data_clone.tile_xp = account.player.tile_data.tile_xp;

    game_data.spawn_player(account.signer.key(), tile_data_clone);

    Ok(())
}

#[derive(Accounts, Session)]
#[instruction(level_seed: String)]
pub struct BuyNextFloor<'info> {
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
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,

    // There can be multiple levels the seed for the level is passed in the instruction
    // First player starting a new level will pay for the account in the current setup
    #[account(
        init,
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
