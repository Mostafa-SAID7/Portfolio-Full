import { useEffect, useRef, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Check, Send } from "lucide-react";
import { Reveal } from "@/components/ui/Reveal";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { LottieAside } from "@/components/ui/LottieAside";
import { SelectField } from "@/components/ui/select-field";
import { useI18n } from "@/lib/i18n";
import { SOCIAL_LINKS } from "@/lib/site";
import {
  submitContact,
  notifyViaFormspree,
  CONTACT_SUBJECT_OPTIONS,
  contactSubjectLabelKey,
  hasKnownSubjectOption,
  isContactSubjectOption,
  SUBJECT_MAX_LENGTH,
  type ContactField,
  type ContactFormValues,
  type ContactSubjectOption,
} from "@/lib/contact";

/**
 * Validation messages are resolved when the form is validated, not when the
 * schema is built, so a visitor who switches language gets the messages in
 * the language they are reading. The holder is kept in sync by the component
 * (see the effect below) — the schema itself stays module-level so the form
 * never has to be re-created.
 */
const validationMessages = {
  name: "Please enter your name",
  nameMax: "Name must not exceed 100 characters",
  email: "Please enter a valid email",
  subject: "Please choose what your message is about.",
  subjectOther: "Please add a short subject",
  subjectMax: "Subject must not exceed 200 characters",
  messageMin: "Message must be at least 10 characters",
  messageMax: "Message must not exceed 1000 characters",
};

/** Limits mirror ContactValidator so the visitor is never told twice. */
const schema = z.object({
  name: z
    .string()
    .min(2, { error: () => validationMessages.name })
    .max(100, { error: () => validationMessages.nameMax }),
  email: z.email({ error: () => validationMessages.email }),
  subjectOption: z.string().refine(hasKnownSubjectOption, {
    error: () => validationMessages.subject,
  }),
  /** Only used by the "Something else" choice, so the field can be absent. */
  subjectOther: z
    .string()
    .max(SUBJECT_MAX_LENGTH, { error: () => validationMessages.subjectMax })
    .optional(),
  message: z
    .string()
    .min(10, { error: () => validationMessages.messageMin })
    .max(1000, { error: () => validationMessages.messageMax }),
  _gotcha: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

type Phase =
  | { kind: "idle" }
  | { kind: "sending" }
  | { kind: "sent"; degraded: boolean }
  | { kind: "failed"; message: string };

const MESSAGE_LIMIT = 1000;

export function Contact({ showHeading = true }: { showHeading?: boolean } = {}) {
  const { tr, lang } = useI18n();
  const [phase, setPhase] = useState<Phase>({ kind: "idle" });
  /** Bumped once per confirmed success — keys the animation so it plays once. */
  const [successKey, setSuccessKey] = useState(0);
  const inFlight = useRef(false);
  const successRef = useRef<HTMLDivElement>(null);

  // Keep the validation messages in the visitor's language. The schema reads
  // them at validation time, so no re-creation of the form is needed.
  useEffect(() => {
    validationMessages.name = tr("contact.validation.name");
    validationMessages.nameMax = tr("contact.validation.nameMax");
    validationMessages.email = tr("contact.validation.email");
    validationMessages.subject = tr("contact.validation.subject");
    validationMessages.subjectOther = tr("contact.validation.subjectOther");
    validationMessages.subjectMax = tr("contact.validation.subjectMax");
    validationMessages.messageMin = tr("contact.validation.messageMin");
    validationMessages.messageMax = tr("contact.validation.messageMax");
  }, [lang, tr]);

  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    // An uncontrolled <select> with no default is auto-advanced by the browser
    // past its disabled placeholder, so the first option would be submitted as
    // if the visitor had chosen it. Seeding "" keeps the placeholder showing
    // and makes the required choice real.
    defaultValues: {
      name: "",
      email: "",
      subjectOption: "",
      subjectOther: "",
      message: "",
      _gotcha: "",
    },
  });

  const sending = phase.kind === "sending" || isSubmitting;

  useEffect(() => {
    if (phase.kind === "sent") successRef.current?.focus();
  }, [phase, successKey]);

  const onSubmit = handleSubmit(async (values) => {
    if (inFlight.current) return;
    // Hidden trap field: bots fill it, people never see it.
    if (values._gotcha) {
      setPhase({ kind: "failed", message: tr("contact.error") });
      return;
    }

    const detail = values.subjectOther?.trim() ?? "";
    if (values.subjectOption === "other" && detail.length < 2) {
      setError("subjectOther", { message: validationMessages.subjectOther });
      return;
    }

    inFlight.current = true;
    setPhase({ kind: "sending" });

    // The label the visitor picked is the subject that gets stored, so the
    // inbox reads in the language they were browsing in.
    const option: ContactSubjectOption = isContactSubjectOption(values.subjectOption)
      ? values.subjectOption
      : "other";
    const payload: ContactFormValues = {
      name: values.name.trim(),
      email: values.email.trim(),
      subject: option === "other" ? detail : tr(contactSubjectLabelKey(option)),
      message: values.message.trim(),
    };

    try {
      // The API is the source of truth: it validates, stores and feeds the inbox.
      const saved = await submitContact(payload);
      if (!saved.ok) {
        for (const [field, message] of Object.entries(saved.fieldErrors)) {
          setError(subjectAwareField(field as ContactField, option), { message });
        }
        setPhase({ kind: "failed", message: saved.message });
        return;
      }

      // Redundant email channel: a failure here must not discard a saved message.
      const notified = await notifyViaFormspree(payload);
      reset({
        name: "",
        email: "",
        subjectOption: "",
        subjectOther: "",
        message: "",
        _gotcha: "",
      });
      setSuccessKey((count) => count + 1);
      setPhase({ kind: "sent", degraded: !notified });
    } finally {
      inFlight.current = false;
    }
  });

  /** A server Subject rejection belongs on whichever control the visitor used. */
  function subjectAwareField(field: ContactField, option: ContactSubjectOption): keyof FormValues {
    if (field !== "subject") return field as keyof FormValues;
    return option === "other" ? "subjectOther" : "subjectOption";
  }

  // `type-control` is what makes the subject select and these inputs the same
  // height: without it the fields inherit 16px/24px text and sit 4px taller.
  const fieldClass =
    "w-full rounded-xl border border-border bg-foreground/10 px-4 py-3 type-control text-foreground outline-none transition-colors placeholder:text-foreground/50 focus:border-primary/60 focus:bg-foreground/15";

  if (phase.kind === "sent") {
    return (
      <section id="contact" className="section-shell section-y-lg scroll-mt-24">
        <div className="container-narrow">
          {showHeading && <SectionHeading title={tr("contact.title")} />}
          <Reveal>
            <div
              ref={successRef}
              tabIndex={-1}
              role="status"
              className="surface-card space-y-5 p-6 text-center outline-none sm:p-10"
            >
              <div className="flex justify-center">
                {/* Pure CSS check: no Lottie runtime, no "Payment Successful"
                    artwork baked in English, and it replays on every success
                    because the key remounts the element. */}
                <span
                  key={successKey}
                  className="check-pop grid size-16 place-items-center rounded-full bg-primary/15 text-primary"
                  aria-hidden="true"
                >
                  <Check className="size-8" strokeWidth={2.5} />
                </span>
              </div>
              <h3 className="type-h3 text-card-foreground">{tr("contact.sent.title")}</h3>
              <p className="mx-auto max-w-md type-body text-card-foreground/70">
                {tr("contact.sent.body")}
              </p>
              {phase.degraded && (
                <p className="mx-auto max-w-md type-body text-muted-foreground">
                  {tr("contact.notify.warning")}
                </p>
              )}
              <button
                type="button"
                onClick={() => setPhase({ kind: "idle" })}
                className="btn-accent type-label"
              >
                {tr("contact.sent.action")}
              </button>
            </div>
          </Reveal>
        </div>
      </section>
    );
  }

  const messageLength = watch("message")?.length ?? 0;
  const needsDetail = watch("subjectOption") === "other";

  return (
    <section id="contact" className="section-shell section-y-lg scroll-mt-24">
      <div className="container-narrow">
        {showHeading && <SectionHeading title={tr("contact.title")} />}
        <Reveal>
          <p className="mb-10 text-center text-foreground/70">{tr("contact.subtitle")}</p>
        </Reveal>

        <LottieAside src="/lottie/contact-mail.lottie">
          <Reveal>
            {/*
              `method=post` + `action=#` is a guard, not a flow: the JS handler
              below is the only thing that ever submits. Without it, a visitor
              who presses Enter before hydration finishes fires a native GET
              submit and the name, email and message land in the URL, the
              history entry and every analytics tool that reads it.
            */}
            <form
              onSubmit={onSubmit}
              method="post"
              action="#"
              className="surface-card space-y-5 p-6 sm:p-8"
              noValidate
            >
              {/* Honeypot: present for bots, hidden for people. */}
              <div className="hidden" aria-hidden="true">
                <label htmlFor="contact-gotcha">Leave this field empty</label>
                <input
                  id="contact-gotcha"
                  tabIndex={-1}
                  autoComplete="off"
                  {...register("_gotcha")}
                  className={fieldClass}
                />
              </div>

              <div>
                <label
                  htmlFor="contact-name"
                  className="mb-2 block type-body-strong text-card-foreground"
                >
                  {tr("contact.name")}
                </label>
                <input
                  id="contact-name"
                  autoComplete="name"
                  aria-required="true"
                  aria-invalid={Boolean(errors.name)}
                  aria-describedby={errors.name ? "contact-name-error" : undefined}
                  {...register("name")}
                  className={fieldClass}
                  placeholder="Jane Doe"
                />
                {errors.name && (
                  <p
                    id="contact-name-error"
                    role="alert"
                    className="mt-1.5 type-body text-destructive"
                  >
                    {errors.name.message}
                  </p>
                )}
              </div>

              <div>
                <label
                  htmlFor="contact-email"
                  className="mb-2 block type-body-strong text-card-foreground"
                >
                  {tr("contact.email")}
                </label>
                <input
                  id="contact-email"
                  type="email"
                  autoComplete="email"
                  aria-required="true"
                  aria-invalid={Boolean(errors.email)}
                  aria-describedby={errors.email ? "contact-email-error" : undefined}
                  {...register("email")}
                  className={fieldClass}
                  placeholder="jane@company.com"
                />
                {errors.email && (
                  <p
                    id="contact-email-error"
                    role="alert"
                    className="mt-1.5 type-body text-destructive"
                  >
                    {errors.email.message}
                  </p>
                )}
              </div>

              <div>
                {/* A fixed list, not free text: every message arrives sortable
                    and the visitor never has to invent a subject line.
                    Rendered with the shared SelectField so the dropdown uses
                    the site's own styling instead of the OS-native popup. */}
                <Controller
                  control={control}
                  name="subjectOption"
                  render={({ field }) => (
                    <SelectField
                      id="contact-subject"
                      label={tr("contact.subject")}
                      value={field.value}
                      onValueChange={field.onChange}
                      options={CONTACT_SUBJECT_OPTIONS.map((option) => ({
                        value: option,
                        label: tr(contactSubjectLabelKey(option)),
                      }))}
                      placeholder={tr("contact.subject.choose")}
                      required
                      invalid={Boolean(errors.subjectOption)}
                      describedBy={errors.subjectOption ? "contact-subject-error" : undefined}
                    />
                  )}
                />
                {errors.subjectOption && (
                  <p
                    id="contact-subject-error"
                    role="alert"
                    className="mt-1.5 type-body text-destructive"
                  >
                    {errors.subjectOption.message}
                  </p>
                )}
              </div>

              {needsDetail && (
                <div>
                  <label
                    htmlFor="contact-subject-other"
                    className="mb-2 block type-body-strong text-card-foreground"
                  >
                    {tr("contact.subject.otherDetail")}
                  </label>
                  <input
                    id="contact-subject-other"
                    autoComplete="off"
                    maxLength={SUBJECT_MAX_LENGTH}
                    aria-required="true"
                    aria-invalid={Boolean(errors.subjectOther)}
                    aria-describedby={
                      errors.subjectOther ? "contact-subject-other-error" : undefined
                    }
                    {...register("subjectOther")}
                    className={fieldClass}
                  />
                  {errors.subjectOther && (
                    <p
                      id="contact-subject-other-error"
                      role="alert"
                      className="mt-1.5 type-body text-destructive"
                    >
                      {errors.subjectOther.message}
                    </p>
                  )}
                </div>
              )}

              <div>
                <label
                  htmlFor="contact-message"
                  className="mb-2 block type-body-strong text-card-foreground"
                >
                  {tr("contact.message")}
                </label>
                <textarea
                  id="contact-message"
                  rows={5}
                  maxLength={MESSAGE_LIMIT}
                  aria-required="true"
                  aria-invalid={Boolean(errors.message)}
                  aria-describedby="contact-message-hint contact-message-count"
                  {...register("message")}
                  className={`${fieldClass} resize-none`}
                  placeholder="Tell me about your marketplace..."
                />
                <div className="mt-1.5 flex items-start justify-between gap-3">
                  {errors.message ? (
                    <p
                      id="contact-message-error"
                      role="alert"
                      className="type-body text-destructive"
                    >
                      {errors.message.message}
                    </p>
                  ) : (
                    <p id="contact-message-hint" className="type-body text-foreground/50">
                      {tr("contact.message.hint")}
                    </p>
                  )}
                  <span
                    id="contact-message-count"
                    className="type-micro shrink-0 text-foreground/50"
                    aria-live="polite"
                  >
                    {messageLength}/{MESSAGE_LIMIT}
                  </span>
                </div>
              </div>

              {phase.kind === "failed" && (
                <div role="alert" className="space-y-2 rounded-xl border border-destructive/40 p-4">
                  <p className="type-body text-destructive">{phase.message}</p>
                  <p className="type-body text-card-foreground/70">{tr("contact.error.kept")}</p>
                  <a
                    href={SOCIAL_LINKS.email.url}
                    className="type-label inline-block text-primary underline-offset-4 hover:underline"
                  >
                    {tr("contact.fallback")}
                  </a>
                </div>
              )}

              <p role="status" aria-live="polite" className="sr-only">
                {sending ? tr("contact.sending") : ""}
              </p>

              <button
                type="submit"
                disabled={sending}
                aria-busy={sending}
                className="group btn-accent type-label w-full"
              >
                {sending ? tr("contact.sending") : tr("contact.send")}
                <Send className="size-4 transition-transform group-hover:translate-x-0.5" />
              </button>
            </form>
          </Reveal>
        </LottieAside>
      </div>
    </section>
  );
}
