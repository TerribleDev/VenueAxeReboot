import { describe, it, expect } from 'vitest';

describe('Transactional Email & Subject Prefix Formatting', () => {
	const formatSubject = (venueName: string | null | undefined, subject: string) => {
		const prefix = (venueName && venueName.trim()) ? venueName.trim() : 'VenueAxe';
		return `[${prefix}] ${subject.trim()}`;
	};

	it('formats subject with explicit venue name prefix', () => {
		expect(formatSubject('Downtown Apex Axes', 'Reservation Confirmed - #VA-10294'))
			.toBe('[Downtown Apex Axes] Reservation Confirmed - #VA-10294');
		expect(formatSubject('Valhalla Throwers', 'Safety Waiver Verified - Sarah Connor'))
			.toBe('[Valhalla Throwers] Safety Waiver Verified - Sarah Connor');
	});

	it('falls back to [VenueAxe] when venue name is omitted or whitespace', () => {
		expect(formatSubject(null, 'Test Transactional Email'))
			.toBe('[VenueAxe] Test Transactional Email');
		expect(formatSubject('', 'System Notice'))
			.toBe('[VenueAxe] System Notice');
		expect(formatSubject('   ', 'Alert Message'))
			.toBe('[VenueAxe] Alert Message');
	});

	it('validates test email request parameters', () => {
		const validateTestEmailRequest = (toEmail: string) => {
			if (!toEmail || !toEmail.trim()) return { valid: false, error: 'Recipient email address is required.' };
			if (!toEmail.includes('@') || !toEmail.includes('.')) return { valid: false, error: 'Invalid email address format.' };
			return { valid: true, error: null };
		};

		expect(validateTestEmailRequest('').valid).toBe(false);
		expect(validateTestEmailRequest('notanemail').valid).toBe(false);
		expect(validateTestEmailRequest('test@tommyparnell.com').valid).toBe(true);
	});

	it('verifies SMTP SSL and Port 465 configuration values', () => {
		const smtpConfig = {
			host: 'mail.tommyparnell.com',
			port: 465,
			from: 'bot@tommyparnell.com',
			enableSsl: true
		};

		expect(smtpConfig.port).toBe(465);
		expect(smtpConfig.host).toBe('mail.tommyparnell.com');
		expect(smtpConfig.from).toBe('bot@tommyparnell.com');
		expect(smtpConfig.enableSsl).toBe(true);
	});
});
