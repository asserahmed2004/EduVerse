using System;
using System.Collections.Generic;

public class UserProfile
{
    public PaymobProfile profile { get; set; }
    public string token { get; set; }
}

public class PaymobProfile
{
    public int id { get; set; }
    public PaymobUser user { get; set; }
    public DateTime created_at { get; set; }
    public bool active { get; set; }
    public string profile_type { get; set; }
    public List<string> phones { get; set; }
    public object company_emails { get; set; }
    public string company_name { get; set; }
    public string state { get; set; }
    public string country { get; set; }
    public string city { get; set; }
    public string postal_code { get; set; }
    public string street { get; set; }
    public bool email_notification { get; set; }
    public object order_retrieval_endpoint { get; set; }
    public object delivery_update_endpoint { get; set; }
    public object logo_url { get; set; }
    public bool is_mobadra { get; set; }
    public string sector { get; set; }
    public bool is_2fa_enabled { get; set; }
    public string otp_sent_to { get; set; }
    public string dashboard_merchant_status { get; set; }
    public int activation_method { get; set; }
    public int signed_up_through { get; set; }
    public int failed_attempts { get; set; }
    public List<object> custom_export_columns { get; set; }
    public List<object> server_IP { get; set; }
    public object username { get; set; }
    public string primary_phone_number { get; set; }
    public bool primary_phone_verified { get; set; }
    public bool is_temp_password { get; set; }
    public object otp_2fa_sent_at { get; set; }
    public object otp_2fa_attempt { get; set; }
    public DateTime otp_sent_at { get; set; }
    public DateTime otp_validated_at { get; set; }
    public object awb_banner { get; set; }
    public object email_banner { get; set; }
    public object identification_number { get; set; }
    public string delivery_status_callback { get; set; }
    public object merchant_external_link { get; set; }
    public int merchant_status { get; set; }
    public bool deactivated_by_bank { get; set; }
    public object bank_deactivation_reason { get; set; }
    public int bank_merchant_status { get; set; }
    public object national_id { get; set; }
    public object super_agent { get; set; }
    public object wallet_limit_profile { get; set; }
    public object address { get; set; }
    public object commercial_registration { get; set; }
    public object commercial_registration_area { get; set; }
    public object distributor_code { get; set; }
    public object distributor_branch_code { get; set; }
    public bool allow_terminal_order_id { get; set; }
    public bool allow_encryption_bypass { get; set; }
    public object wallet_phone_number { get; set; }
    public int suspicious { get; set; }
    public object latitude { get; set; }
    public object longitude { get; set; }
    public Dictionary<string, object> bank_staffs { get; set; }
    public object bank_rejection_reason { get; set; }
    public bool bank_received_documents { get; set; }
    public int bank_merchant_digital_status { get; set; }
    public object bank_digital_rejection_reason { get; set; }
    public bool filled_business_data { get; set; }
    public string day_start_time { get; set; }
    public object day_end_time { get; set; }
    public bool withhold_transfers { get; set; }
    public bool manual_settlement { get; set; }
    public string sms_sender_name { get; set; }
    public object withhold_transfers_reason { get; set; }
    public object withhold_transfers_notes { get; set; }
    public bool can_bill_deposit_with_card { get; set; }
    public bool can_topup_merchants { get; set; }
    public object topup_transfer_id { get; set; }
    public bool referral_eligible { get; set; }
    public bool is_eligible_to_be_ranger { get; set; }
    public bool eligible_for_manual_refunds { get; set; }
    public bool is_ranger { get; set; }
    public bool is_poaching { get; set; }
    public bool paymob_app_merchant { get; set; }
    public object settlement_frequency { get; set; }
    public object day_of_the_week { get; set; }
    public object day_of_the_month { get; set; }
    public bool allow_transaction_notifications { get; set; }
    public bool allow_transfer_notifications { get; set; }
    public int sallefny_amount_whole { get; set; }
    public int sallefny_fees_whole { get; set; }
    public object paymob_app_first_login { get; set; }
    public object paymob_app_last_activity { get; set; }
    public bool payout_enabled { get; set; }
    public bool payout_terms { get; set; }
    public bool is_bills_new { get; set; }
    public bool can_process_multiple_refunds { get; set; }
    public int settlement_classification { get; set; }
    public int vat_classification { get; set; }
    public bool instant_settlement_enabled { get; set; }
    public bool instant_settlement_transaction_otp_verified { get; set; }
    public object preferred_language { get; set; }
    public bool ignore_flash_callbacks { get; set; }
    public bool receive_callback_card_info { get; set; }
    public object onboarding_freshdesk_ticket_id { get; set; }
    public bool paymob_event { get; set; }
    public object acq_partner { get; set; }
    public object dom { get; set; }
    public object bank_related { get; set; }
    public object settlement_classification_profile { get; set; }
    public List<object> permissions { get; set; }
}

public class PaymobUser
{
    public int id { get; set; }
    public string username { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public DateTime date_joined { get; set; }
    public string email { get; set; }
    public bool is_active { get; set; }
    public bool is_staff { get; set; }
    public bool is_superuser { get; set; }
    public object last_login { get; set; }
    public List<int> user_permissions { get; set; }
    public List<object> groups { get; set; }
}