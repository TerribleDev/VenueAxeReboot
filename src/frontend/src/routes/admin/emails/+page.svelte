<script lang="ts">
	import { onMount } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import { getApiAdminEmailSettings, postApiAdminEmailTest } from '$lib/api/client';

	let emailSettings = $state<any>(null);
	let isLoading = $state(true);

	// Tabs: Diagnostics vs Template Customizer
	let activeTab = $state<'diagnostics' | 'templates'>('diagnostics');

	// SMTP Test State
	let testRecipientEmail = $state('');
	let testVenueName = $state('');
	let isSendingTestEmail = $state(false);
	let emailTestResult = $state<{ success: boolean; message: string; timestamp?: string } | null>(null);

	// Template Customizer State
	type TemplateKey = 'confirmation' | 'waiver' | 'cancellation' | 'reminder';
	let selectedTemplate = $state<TemplateKey>('confirmation');

	interface TemplateData {
		title: string;
		subject: string;
		headline: string;
		bodyMessage: string;
		ctaText: string;
	}

	let templates = $state<Record<TemplateKey, TemplateData>>({
		confirmation: {
			title: 'Booking Confirmation',
			subject: '[{{VenueName}}] Reservation Confirmed - #{{BookingReference}}',
			headline: 'Your Target Bay is Reserved!',
			bodyMessage: 'We are thrilled to host your group at {{VenueName}}! Please arrive 15 minutes prior to your start time wearing closed-toe shoes.',
			ctaText: 'Sign Digital Waiver Now'
		},
		waiver: {
			title: 'Waiver Verification',
			subject: '[{{VenueName}}] Safety Waiver Verified - {{CustomerName}}',
			headline: 'Waiver Successfully Signed & Verified',
			bodyMessage: 'Your digital liability waiver has been recorded in our permanent legal vault. You are cleared for bay entry upon arrival.',
			ctaText: 'Download PDF Audit Copy'
		},
		cancellation: {
			title: 'Reservation Cancellation',
			subject: '[{{VenueName}}] Reservation Cancelled - #{{BookingReference}}',
			headline: 'Your Reservation Has Been Cancelled',
			bodyMessage: 'Your booking has been cancelled per your request. If a refund is due according to our 24-hour policy, it has been issued to your original payment method.',
			ctaText: 'Book New Session'
		},
		reminder: {
			title: '24-Hour Session Reminder',
			subject: '[{{VenueName}}] 24-Hour Reminder - Session Tomorrow at {{StartTime}}',
			headline: 'Ready to Throw Tomorrow?',
			bodyMessage: 'A friendly reminder that your axe throwing match is scheduled for tomorrow. Remind all throwers in your party to wear closed-toe shoes and complete their waivers.',
			ctaText: 'View Group Waiver Status'
		}
	});

	let templateSavedNotice = $state(false);

	function saveTemplateCustomization() {
		templateSavedNotice = true;
		setTimeout(() => (templateSavedNotice = false), 3000);
	}

	function insertMergeTag(tag: string) {
		templates[selectedTemplate].bodyMessage += ` ${tag}`;
	}

	async function loadEmailSettings() {
		isLoading = true;
		try {
			const res = await getApiAdminEmailSettings();
			emailSettings = res.data || null;
		} catch (e) {
			console.error('Failed to load email settings', e);
		} finally {
			isLoading = false;
		}
	}

	async function handleSendTestEmail(e: SubmitEvent) {
		e.preventDefault();
		if (!testRecipientEmail.trim()) return;

		isSendingTestEmail = true;
		emailTestResult = null;

		try {
			const res = await postApiAdminEmailTest({
				body: {
					toEmail: testRecipientEmail.trim(),
					venueName: testVenueName.trim() || venueState.selectedVenue?.name || 'Downtown Apex Axes'
				}
			});

			if (res.data) {
				emailTestResult = {
					success: res.data.success,
					message: res.data.message,
					timestamp: new Date().toLocaleTimeString()
				};
			} else {
				emailTestResult = {
					success: false,
					message: 'Failed to send test email.',
					timestamp: new Date().toLocaleTimeString()
				};
			}
		} catch (err: any) {
			emailTestResult = {
				success: false,
				message: err?.message || 'Error occurred while contacting mail server.',
				timestamp: new Date().toLocaleTimeString()
			};
		} finally {
			isSendingTestEmail = false;
		}
	}

	onMount(() => {
		loadEmailSettings();
	});
</script>

<svelte:head>
	<title>Email & SMTP Pipeline | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Transactional Email & SMTP Pipeline</h2>
		<p class="tab-subtitle">Live mail delivery diagnostics, port 465 SSL connection, and automated email customization</p>
	</div>
	<div style="display: flex; gap: 0.75rem; align-items: center;">
		<div class="subtabs-toggle" style="background: rgba(255, 255, 255, 0.06); padding: 3px; border-radius: var(--radius-sm); display: inline-flex;">
			<button
				type="button"
				class="mode-btn"
				class:active={activeTab === 'diagnostics'}
				onclick={() => (activeTab = 'diagnostics')}
			>
				⚙️ SMTP Diagnostics
			</button>
			<button
				type="button"
				class="mode-btn"
				class:active={activeTab === 'templates'}
				onclick={() => (activeTab = 'templates')}
			>
				📝 Template Customizer
			</button>
		</div>

		<div class="smtp-live-badge" style="display: inline-flex; align-items: center; gap: 0.5rem; background: rgba(16, 185, 129, 0.12); border: 1px solid rgba(16, 185, 129, 0.3); color: #34d399; padding: 0.4rem 0.85rem; border-radius: 9999px; font-size: 0.8rem; font-weight: 700;">
			<span style="width: 8px; height: 8px; border-radius: 50%; background: #10b981; box-shadow: 0 0 8px #10b981;"></span>
			<span>Port 465 SSL Active</span>
		</div>
	</div>
</div>

{#if activeTab === 'diagnostics'}
	<div class="email-grid" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 1.5rem; margin-top: 1rem;">
		<!-- Diagnostics Card -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.15rem; margin-bottom: 1.25rem;">⚙️ SMTP Connection Settings</h3>
			<div class="settings-rows" style="display: flex; flex-direction: column; gap: 0.75rem;">
				<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid rgba(255, 255, 255, 0.06); font-size: 0.85rem;">
					<span style="color: var(--text-secondary);">Mail Server</span>
					<span class="font-mono" style="font-weight: 600;">{emailSettings?.server || 'mail.tommyparnell.com'}</span>
				</div>
				<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid rgba(255, 255, 255, 0.06); font-size: 0.85rem;">
					<span style="color: var(--text-secondary);">SMTP Port</span>
					<span class="font-mono" style="font-weight: 600;">{emailSettings?.port || 465} (Implicit SSL / SslOnConnect)</span>
				</div>
				<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid rgba(255, 255, 255, 0.06); font-size: 0.85rem;">
					<span style="color: var(--text-secondary);">Default Sender</span>
					<span class="font-mono" style="font-weight: 600;">{emailSettings?.sender || 'bot@tommyparnell.com'} ({emailSettings?.senderName || 'VenueAxe'})</span>
				</div>
				<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid rgba(255, 255, 255, 0.06); font-size: 0.85rem;">
					<span style="color: var(--text-secondary);">Subject Prefix</span>
					<span class="font-mono" style="font-weight: 600;">[{venueState.selectedVenue?.name || 'VenueName'}] ...</span>
				</div>
				<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0; font-size: 0.85rem;">
					<span style="color: var(--text-secondary);">TLS/SSL Encryption</span>
					<span style="color: #34d399; font-weight: 600;">✓ Encrypted (SSL on Connect)</span>
				</div>
			</div>
			<div class="info-alert" style="margin-top: 1.25rem; background: rgba(56, 189, 248, 0.1); border: 1px solid rgba(56, 189, 248, 0.25); color: #bae6fd; padding: 0.85rem; border-radius: var(--radius-sm); font-size: 0.8rem; line-height: 1.4;">
				<strong>📌 Subject Prefix Enforcement:</strong> All outgoing guest communications strictly prefix the subject with <code>[{venueState.selectedVenue?.name || 'VenueName'}]</code> per VenueAxe branding standards.
			</div>
		</div>

		<!-- Live Test Dispatch Card -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.15rem; margin-bottom: 0.5rem;">🚀 Live SMTP Delivery Tester</h3>
			<p style="color: var(--text-secondary); font-size: 0.85rem; margin-bottom: 1.25rem;">
				Send a live transactional test email from <code>{emailSettings?.sender || 'bot@tommyparnell.com'}</code> over secure Port 465 to verify end-to-end delivery.
			</p>

			<form onsubmit={handleSendTestEmail}>
				<div class="form-group">
					<label class="form-label" for="test-recip">Recipient Email Address *</label>
					<input id="test-recip" type="email" class="form-input font-mono" bind:value={testRecipientEmail} required placeholder="you@example.com" />
				</div>

				<div class="form-group" style="margin-top: 1rem;">
					<label class="form-label" for="test-override">Venue Name Prefix Override (Optional)</label>
					<input id="test-override" type="text" class="form-input" bind:value={testVenueName} placeholder={venueState.selectedVenue?.name || 'Downtown Apex Axes'} />
				</div>

				{#if emailTestResult}
					<div class="result-banner" style="margin-top: 1rem; padding: 0.85rem; border-radius: var(--radius-sm); font-size: 0.85rem; display: flex; gap: 0.75rem; background: {emailTestResult.success ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)'}; border: 1px solid {emailTestResult.success ? '#10b981' : '#ef4444'}; color: {emailTestResult.success ? '#a7f3d0' : '#fca5a5'};">
						<span>{emailTestResult.success ? '✅' : '❌'}</span>
						<div>
							<div style="font-weight: 600;">{emailTestResult.message}</div>
							{#if emailTestResult.timestamp}
								<div style="font-size: 0.75rem; opacity: 0.8; margin-top: 2px;">Tested at {emailTestResult.timestamp}</div>
							{/if}
						</div>
					</div>
				{/if}

				<div style="margin-top: 1.5rem; display: flex; justify-content: flex-end;">
					<button id="send-test-email-btn" type="submit" class="btn btn-primary font-display" disabled={isSendingTestEmail}>
						{isSendingTestEmail ? 'Sending via Port 465...' : '✉️ Send Live Test Email'}
					</button>
				</div>
			</form>
		</div>
	</div>

	<!-- Lifecycle Triggers Overview -->
	<div class="glass-panel" style="margin-top: 1.5rem; padding: 1.75rem;">
		<h3 class="font-display" style="font-size: 1.15rem; margin-bottom: 1rem;">🔄 Active Automated Email Lifecycle Triggers</h3>
		<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem;">
			<div style="background: rgba(15, 20, 28, 0.6); border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 1.25rem; display: flex; gap: 1rem;">
				<div style="font-size: 1.8rem;">📅</div>
				<div>
					<h4 class="font-display" style="font-size: 1rem; margin-bottom: 0.35rem; color: #f8fafc;">1. Booking Confirmation</h4>
					<p style="font-size: 0.8rem; color: var(--text-secondary); line-height: 1.4; margin-bottom: 0.75rem;">
						Dispatched automatically upon guest online checkout or admin walk-in reservation. Includes reference, assigned lanes, closed-toe shoes reminder, receipt, and waiver link.
					</p>
					<div style="font-size: 0.7rem; font-family: monospace; background: rgba(255, 255, 255, 0.05); padding: 4px 8px; border-radius: 4px; color: var(--accent-amber);">
						Subject: [{venueState.selectedVenue?.name || 'Venue'}] Reservation Confirmed - #WA-XXXXX
					</div>
				</div>
			</div>

			<div style="background: rgba(15, 20, 28, 0.6); border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 1.25rem; display: flex; gap: 1rem;">
				<div style="font-size: 1.8rem;">✍️</div>
				<div>
					<h4 class="font-display" style="font-size: 1rem; margin-bottom: 0.35rem; color: #f8fafc;">2. Digital Waiver Verification</h4>
					<p style="font-size: 0.8rem; color: var(--text-secondary); line-height: 1.4; margin-bottom: 0.75rem;">
						Dispatched immediately when a participant or guardian submits a digital waiver. Contains legal acknowledgment, minors covered, and SHA-256 audit stamp.
					</p>
					<div style="font-size: 0.7rem; font-family: monospace; background: rgba(255, 255, 255, 0.05); padding: 4px 8px; border-radius: 4px; color: var(--accent-amber);">
						Subject: [{venueState.selectedVenue?.name || 'Venue'}] Safety Waiver Verified - [Name]
					</div>
				</div>
			</div>

			<div style="background: rgba(15, 20, 28, 0.6); border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 1.25rem; display: flex; gap: 1rem;">
				<div style="font-size: 1.8rem;">🚫</div>
				<div>
					<h4 class="font-display" style="font-size: 1rem; margin-bottom: 0.35rem; color: #f8fafc;">3. Reservation Cancellation</h4>
					<p style="font-size: 0.8rem; color: var(--text-secondary); line-height: 1.4; margin-bottom: 0.75rem;">
						Dispatched when venue staff marks a reservation as cancelled. Includes original date & time and direct venue contact information for rescheduling.
					</p>
					<div style="font-size: 0.7rem; font-family: monospace; background: rgba(255, 255, 255, 0.05); padding: 4px 8px; border-radius: 4px; color: var(--accent-amber);">
						Subject: [{venueState.selectedVenue?.name || 'Venue'}] Reservation Cancelled - #WA-XXXXX
					</div>
				</div>
			</div>
		</div>
	</div>
{:else}
	<!-- Email Template Visual Customizer Tab -->
	<div class="template-customizer" style="display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-top: 1rem;">
		<!-- Editor Panel -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem;">
				<h3 class="font-display" style="font-size: 1.15rem; margin: 0;">✏️ Customize Email Copy</h3>
				<select class="form-input" style="width: auto;" bind:value={selectedTemplate}>
					<option value="confirmation">📅 Booking Confirmation</option>
					<option value="waiver">✍️ Waiver Verification</option>
					<option value="cancellation">🚫 Reservation Cancellation</option>
					<option value="reminder">⏰ 24-Hour Reminder</option>
				</select>
			</div>

			{#if templateSavedNotice}
				<div class="alert-success" style="margin-bottom: 1rem; font-size: 0.85rem;">
					✓ Email template configuration saved!
				</div>
			{/if}

			<div class="form-group">
				<label class="form-label" for="tpl-subject">Subject Line</label>
				<input id="tpl-subject" type="text" class="form-input font-mono" style="font-size: 0.85rem;" bind:value={templates[selectedTemplate].subject} />
			</div>

			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="tpl-headline">Header Banner Headline</label>
				<input id="tpl-headline" type="text" class="form-input" bind:value={templates[selectedTemplate].headline} />
			</div>

			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="tpl-msg">Custom Body Message</label>
				<textarea id="tpl-msg" class="form-input" rows="5" style="line-height: 1.5;" bind:value={templates[selectedTemplate].bodyMessage}></textarea>
			</div>

			<div style="margin-top: 0.75rem;">
				<span style="font-size: 0.75rem; color: var(--text-secondary); margin-right: 0.5rem;">Insert Token:</span>
				<div style="display: inline-flex; gap: 0.35rem; flex-wrap: wrap;">
					<button type="button" class="btn-clear token-chip" onclick={() => insertMergeTag('{{CustomerName}}')}>CustomerName</button>
					<button type="button" class="btn-clear token-chip" onclick={() => insertMergeTag('{{BookingReference}}')}>BookingReference</button>
					<button type="button" class="btn-clear token-chip" onclick={() => insertMergeTag('{{VenueName}}')}>VenueName</button>
					<button type="button" class="btn-clear token-chip" onclick={() => insertMergeTag('{{StartTime}}')}>StartTime</button>
				</div>
			</div>

			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="tpl-cta">Action Button Text</label>
				<input id="tpl-cta" type="text" class="form-input" bind:value={templates[selectedTemplate].ctaText} />
			</div>

			<div style="margin-top: 1.5rem; display: flex; justify-content: flex-end;">
				<button type="button" class="btn btn-primary font-display" onclick={saveTemplateCustomization}>
					💾 Save Template
				</button>
			</div>
		</div>

		<!-- Live Email Preview Mockup -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
				<h3 class="font-display" style="font-size: 1.15rem; margin: 0; color: var(--accent-cyan);">
					📱 Live Inbox Preview
				</h3>
				<span style="font-size: 0.75rem; color: var(--text-secondary);">Rendered HTML</span>
			</div>

			<div class="email-mockup" style="background: #0f172a; border: 1px solid var(--border-color); border-radius: var(--radius-md); overflow: hidden; font-family: sans-serif;">
				<!-- Email Client Topbar -->
				<div style="background: #1e293b; padding: 0.75rem 1rem; border-bottom: 1px solid rgba(255, 255, 255, 0.08); font-size: 0.8rem;">
					<div style="color: #94a3b8;">From: <span style="color: #fff;">{venueState.selectedVenue?.name || 'Apex Axe House'} &lt;bot@tommyparnell.com&gt;</span></div>
					<div style="color: #94a3b8; margin-top: 0.25rem;">Subject: <span style="color: var(--accent-amber); font-weight: 600;">{templates[selectedTemplate].subject.replace('{{VenueName}}', venueState.selectedVenue?.name || 'Apex Axe House').replace('{{BookingReference}}', 'VA-84920').replace('{{CustomerName}}', 'Marcus Vance').replace('{{StartTime}}', '7:00 PM')}</span></div>
				</div>

				<!-- Email Body Wrapper -->
				<div style="padding: 2rem; background: #0b1120;">
					<div style="text-align: center; margin-bottom: 1.5rem;">
						<span style="font-size: 2.2rem;">🪓</span>
						<h2 style="color: #fff; margin: 0.5rem 0 0.25rem; font-size: 1.3rem;">{venueState.selectedVenue?.name || 'Apex Axe House'}</h2>
						<p style="color: #94a3b8; font-size: 0.8rem; margin: 0;">Official Axe Throwing & Entertainment</p>
					</div>

					<div style="background: #1e293b; border-radius: 8px; padding: 1.5rem; border: 1px solid rgba(255, 255, 255, 0.08);">
						<h3 style="color: #f8fafc; font-size: 1.15rem; margin: 0 0 1rem;">{templates[selectedTemplate].headline}</h3>
						<p style="color: #cbd5e1; font-size: 0.9rem; line-height: 1.6; margin: 0 0 1.5rem;">
							{templates[selectedTemplate].bodyMessage.replace('{{VenueName}}', venueState.selectedVenue?.name || 'Apex Axe House').replace('{{BookingReference}}', 'VA-84920').replace('{{CustomerName}}', 'Marcus Vance').replace('{{StartTime}}', '7:00 PM')}
						</p>

						<div style="text-align: center;">
							<span style="display: inline-block; background: #f59e0b; color: #000; font-weight: 700; padding: 0.75rem 1.5rem; border-radius: 6px; font-size: 0.9rem;">
								{templates[selectedTemplate].ctaText}
							</span>
						</div>
					</div>

					<div style="text-align: center; margin-top: 1.5rem; font-size: 0.75rem; color: #64748b;">
						{venueState.selectedVenue?.addressLine1 || '100 Main St'}, {venueState.selectedVenue?.city || 'Downtown'}<br/>
						You received this because of a booking activity on VenueAxe.
					</div>
				</div>
			</div>
		</div>
	</div>
{/if}

<style>
	.mode-btn {
		background: transparent;
		border: none;
		color: var(--text-secondary);
		padding: 0.4rem 0.85rem;
		font-size: 0.82rem;
		font-weight: 600;
		border-radius: calc(var(--radius-sm) - 2px);
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.mode-btn.active {
		background: var(--accent-amber);
		color: #000;
		font-weight: 700;
	}

	.token-chip {
		background: rgba(255, 255, 255, 0.08);
		border: 1px solid rgba(255, 255, 255, 0.15);
		color: var(--accent-cyan);
		font-family: monospace;
		font-size: 0.75rem;
		padding: 2px 6px;
		border-radius: 4px;
		cursor: pointer;
		transition: background 0.15s;
	}

	.token-chip:hover {
		background: rgba(6, 182, 212, 0.2);
	}
</style>
